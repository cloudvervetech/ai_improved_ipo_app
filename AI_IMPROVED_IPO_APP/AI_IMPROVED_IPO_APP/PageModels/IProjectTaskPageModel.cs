using AI_IMPROVED_IPO_APP.Models;
using CommunityToolkit.Mvvm.Input;

namespace AI_IMPROVED_IPO_APP.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}