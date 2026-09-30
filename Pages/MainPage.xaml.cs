using System.Globalization;

namespace Calculatrice_De_Dorcas.Pages;

public partial class MainPage : ContentPage
{
    private const int MaxDigits = 15;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // État de la calculatrice
    private string _input = "0";      // saisie courante (séparateur décimal interne : '.')
    private decimal? _left;           // premier opérande
    private string? _op;              // opérateur en attente (+ − × ÷)
    private bool _resetInput;         // la prochaine saisie remplace l'affichage
    private bool _afterEquals;        // le dernier appui était "="
    private bool _error;              // état d'erreur (ex. division par zéro)

    private bool? _isLandscape;

    public MainPage()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    // ------------------------------------------------------------------
    // Adaptation à l'orientation : portrait = affichage au-dessus du clavier,
    // paysage = affichage à gauche, clavier à droite.
    // ------------------------------------------------------------------
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;

        bool landscape = width > height;
        if (_isLandscape == landscape) return;
        _isLandscape = landscape;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (landscape)
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.4, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Grid.SetRow(DisplayPanel, 0); Grid.SetColumn(DisplayPanel, 0);
            Grid.SetRow(KeypadGrid, 0); Grid.SetColumn(KeypadGrid, 1);
        }
        else
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(2, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(5, GridUnitType.Star)));

            Grid.SetRow(DisplayPanel, 0); Grid.SetColumn(DisplayPanel, 0);
            Grid.SetRow(KeypadGrid, 1); Grid.SetColumn(KeypadGrid, 0);
        }
    }

    // ------------------------------------------------------------------
    // Gestionnaires d'événements
    // ------------------------------------------------------------------
    private void OnDigitClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b) return;
        if (_error) ResetAll();

        if (_afterEquals)
        {
            ExpressionLabel.Text = string.Empty;
            _afterEquals = false;
        }

        if (_resetInput)
        {
            _input = "0";
            _resetInput = false;
        }

        // Limite le nombre de chiffres pour éviter tout débordement
        if (_input.Count(char.IsDigit) >= MaxDigits) return;

        _input = _input == "0" ? b.Text : _input + b.Text;
        UpdateDisplay();
    }

    private void OnDecimalClicked(object? sender, EventArgs e)
    {
        if (_error) ResetAll();

        if (_afterEquals)
        {
            ExpressionLabel.Text = string.Empty;
            _afterEquals = false;
        }

        if (_resetInput)
        {
            _input = "0";
            _resetInput = false;
        }

        if (_input.Contains('.')) return;   // un seul séparateur décimal
        _input += ".";
        UpdateDisplay();
    }

    private void OnOperatorClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || _error) return;
        string newOp = b.Text;

        // Changement d'opérateur avant d'avoir saisi le second nombre
        if (_op != null && _resetInput && !_afterEquals)
        {
            _op = newOp;
            ExpressionLabel.Text = $"{FormatNumber(_left!.Value)} {_op}";
            UpdateDisplay(keepInput: true);
            return;
        }

        decimal current = ParseInput();

        // Enchaînement : 2 + 3 + → calcule 5 puis attend la suite
        if (_op != null && _left != null && !_resetInput)
        {
            if (!TryCompute(_left.Value, current, _op, out decimal partial)) return;
            _left = partial;
            _input = FormatNumber(partial, internalFormat: true);
        }
        else
        {
            _left = current;
        }

        _op = newOp;
        _resetInput = true;
        _afterEquals = false;
        ExpressionLabel.Text = $"{FormatNumber(_left.Value)} {_op}";
        UpdateDisplay();
    }

    private void OnEqualsClicked(object? sender, EventArgs e)
    {
        if (_error || _op == null || _left == null) return;

        decimal right = ParseInput();
        decimal left = _left.Value;
        string op = _op;
        string expression = $"{FormatNumber(left)} {op} {FormatNumber(right)} =";

        if (!TryCompute(left, right, op, out decimal result)) return;

        ExpressionLabel.Text = expression;
        _input = FormatNumber(result, internalFormat: true);
        _left = null;
        _op = null;
        _resetInput = true;
        _afterEquals = true;
        UpdateDisplay();
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        ResetAll();
        UpdateDisplay();
    }

    private void OnBackspaceClicked(object? sender, EventArgs e)
    {
        if (_error) { ResetAll(); UpdateDisplay(); return; }
        if (_resetInput) return;   // on n'efface pas un résultat calculé

        _input = _input.Length > 1 ? _input[..^1] : "0";
        if (_input == "-" || _input == "-0") _input = "0";
        UpdateDisplay();
    }

    private void OnSignClicked(object? sender, EventArgs e)
    {
        if (_error) return;
        if (ParseInput() == 0m && !_input.Contains('.')) return;   // pas de "-0"

        _input = _input.StartsWith('-') ? _input[1..] : "-" + _input;
        if (_resetInput && !_afterEquals) _resetInput = false;
        UpdateDisplay();
    }

    private void OnPercentClicked(object? sender, EventArgs e)
    {
        if (_error) return;

        decimal current = ParseInput();
        decimal value;

        // Avec une opération en attente : 200 + 10 % → 10 % de 200 = 20
        if (_op != null && _left != null && (_op == "+" || _op == "−"))
            value = _left.Value * current / 100m;
        else
            value = current / 100m;

        _input = FormatNumber(value, internalFormat: true);
        _resetInput = false;
        UpdateDisplay();
    }

    // ------------------------------------------------------------------
    // Logique interne
    // ------------------------------------------------------------------
    private bool TryCompute(decimal a, decimal b, string op, out decimal result)
    {
        result = 0m;
        try
        {
            switch (op)
            {
                case "+": result = a + b; break;
                case "−": result = a - b; break;
                case "×": result = a * b; break;
                case "÷":
                    if (b == 0m)
                    {
                        ShowError("Division par zéro impossible");
                        return false;
                    }
                    result = a / b;
                    break;
            }
            return true;
        }
        catch (OverflowException)
        {
            ShowError("Résultat trop grand");
            return false;
        }
    }

    private void ShowError(string message)
    {
        _error = true;
        _left = null;
        _op = null;
        _input = "0";
        _resetInput = true;
        _afterEquals = false;
        ExpressionLabel.Text = string.Empty;

        ResultLabel.Text = message;
        ResultLabel.FontSize = 26;
        ResultLabel.TextColor = Color.FromArgb("#FF453A");
        StatusLabel.Text = "● Erreur";
        StatusLabel.TextColor = Color.FromArgb("#FF453A");
    }

    private void ResetAll()
    {
        _input = "0";
        _left = null;
        _op = null;
        _resetInput = false;
        _afterEquals = false;
        _error = false;
        ExpressionLabel.Text = string.Empty;
    }

    private decimal ParseInput()
        => decimal.TryParse(_input, NumberStyles.Float, Inv, out var v) ? v : 0m;

    /// <summary>Formate un nombre. internalFormat = true → séparateur '.', sinon ',' pour l'affichage.</summary>
    private static string FormatNumber(decimal value, bool internalFormat = false)
    {
        value = decimal.Round(value, 10, MidpointRounding.AwayFromZero);
        string s = value.ToString("0.##########", Inv);
        if (s == "-0") s = "0";
        return internalFormat ? s : s.Replace('.', ',');
    }

    private void UpdateDisplay(bool keepInput = false)
    {
        if (!_error)
        {
            string text = _input.Replace('.', ',');
            ResultLabel.Text = text;
            ResultLabel.TextColor = Colors.White;
            StatusLabel.Text = "● Prêt";
            StatusLabel.TextColor = Color.FromArgb("#30D158");

            // Réduction progressive de la police pour les longs nombres
            int len = text.Length;
            ResultLabel.FontSize = len <= 8 ? 56 : len <= 11 ? 44 : len <= 14 ? 34 : 28;
        }

        // Les ScrollView horizontaux se calent sur la fin du texte
        Dispatcher.Dispatch(async () =>
        {
            await ResultScroll.ScrollToAsync(ResultScroll.ContentSize.Width, 0, false);
            await ExpressionScroll.ScrollToAsync(ExpressionScroll.ContentSize.Width, 0, false);
        });
    }
}
