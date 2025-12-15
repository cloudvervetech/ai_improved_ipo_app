using AI_IMPROVED_IPO_APP.Models;
using AI_IMPROVED_IPO_APP.PageModels;

namespace AI_IMPROVED_IPO_APP.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}