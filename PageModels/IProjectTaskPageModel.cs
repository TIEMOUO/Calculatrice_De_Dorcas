using Calculatrice_De_Dorcas.Models;
using CommunityToolkit.Mvvm.Input;

namespace Calculatrice_De_Dorcas.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}