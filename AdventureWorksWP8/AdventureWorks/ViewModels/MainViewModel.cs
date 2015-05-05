using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Geolocation;
using Windows.Media.SpeechRecognition;
using BuildSessions.Common;
using BuildSessions.DataModel;
using BuildSessions.Views;

namespace BuildSessions.ViewModels
{
    public class MainViewModel: BaseViewModel
    {
        private SpeechRecognizer SpeechRecognizer;

        #region Bindable properties

        public ObservableCollection<ItemViewModel> Items { get; private set; }

        private string _SearchType;
        public string SearchType
        {
            get { return _SearchType; }
            set { RaisePropertyChanged(ref _SearchType, value); }
        }
        

        private int _SelectedIndex;
        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set { RaisePropertyChanged(ref _SelectedIndex, value); }
        }
        

        private string _SearchTerm;
        public string SearchTerm
        {
            get { return _SearchTerm; }
            set { RaisePropertyChanged(ref _SearchTerm, value); }
        }

        private bool _IsLoading;
        public bool IsLoading
        {
            get { return _IsLoading; }
            set { RaisePropertyChanged(ref _IsLoading, value); }
        }

        #endregion

        #region Bindable Commands
        public ICommand SearchSessionsCommand
        {
            get { 
                return new RelayCommand(async () => 
                { 
                    Navigate(typeof(ResultsPage));
                    await SearchSessions();
                }); 
            }
        }

        public ICommand ComingNextCommand
        {
            get { 
                return new RelayCommand(async () => 
                { 
                    Navigate(typeof(ResultsPage));
                    await SearchComingNext();
                }); 
            }
        }

        public ICommand RecognizeSpeechCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    await SpeechRecognizer.CompileConstraintsAsync();
                    Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = await SpeechRecognizer.RecognizeWithUIAsync();
                    SearchTerm = speechRecognitionResult.Text.Replace('.', ' ');
                });
            }
        }

        public ICommand LoadItemCommand
        {
            get { return new RelayCommand(() => Navigate(typeof(ItemPage), SelectedIndex)); }
        }

        #endregion

        public MainViewModel()
        {
            Items = new ObservableCollection<ItemViewModel>();
            SpeechRecognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();
        }

        private async Task SearchSessions()
        {
            SearchType = String.Format("Search results for {0}", SearchTerm);
            IsLoading = true;
            var results = await SearchService.SearchAsync(SearchTerm);
            AddSearchResults(results);
        }

        private async Task SearchComingNext()
        {
            SearchType = "Sessions coming next";
            IsLoading = true;
            var results = await SearchService.SearchComingNextAsync();
            AddSearchResults(results);
        }

        private void AddSearchResults(IEnumerable<ItemViewModel> results)
        {
            Items.Clear();
            foreach (var result in results)
            {
                Items.Add(result);
            }
            IsLoading = false;
        }
    }
}
