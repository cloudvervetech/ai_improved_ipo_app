using AI_IMPROVED_IPO_APP.Models;

namespace AI_IMPROVED_IPO_APP.Pages
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
