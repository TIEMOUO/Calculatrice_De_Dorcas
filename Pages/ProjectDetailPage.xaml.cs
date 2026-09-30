using Calculatrice_De_Dorcas.Models;

namespace Calculatrice_De_Dorcas.Pages
{
    public partial class ProjectDetailPage : ContentPage
    {
        public ProjectDetailPage(ProjectDetailPageModel model)
        {
            InitializeComponent();

            BindingContext = model;
        }
    }
}
