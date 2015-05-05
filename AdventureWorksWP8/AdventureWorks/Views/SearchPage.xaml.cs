namespace BuildSessions.Views
{
    public sealed partial class SearchPage : BasePage
    {
        public SearchPage()
        {
            InitializeComponent();
            DataContext = App.MainViewModel; 
        }
    }
}
