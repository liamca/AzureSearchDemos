namespace BuildSessions.Views
{
    public sealed partial class ResultsPage : BasePage
    {
        public ResultsPage()
        {
            InitializeComponent();
            DataContext = App.MainViewModel; 
        }
    }
}
