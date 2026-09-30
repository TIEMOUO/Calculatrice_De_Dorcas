using Syncfusion.Maui.Toolkit.Charts;

namespace Calculatrice_De_Dorcas.Pages.Controls
{
    public class LegendExt : ChartLegend
    {
        protected override double GetMaximumSizeCoefficient()
        {
            return 0.5;
        }
    }
}
