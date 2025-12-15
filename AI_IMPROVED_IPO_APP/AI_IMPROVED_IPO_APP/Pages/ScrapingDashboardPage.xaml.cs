using AI_IMPROVED_IPO_APP.PageModels;

namespace AI_IMPROVED_IPO_APP.Pages
{
    public partial class ScrapingDashboardPage : ContentPage
    {
        public ScrapingDashboardPage(ScrapingDashboardPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
