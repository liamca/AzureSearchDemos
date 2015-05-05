using System;
using System.Collections.Generic;

namespace BuildSessions.ViewModels
{
    public class ItemViewModel : BaseViewModel
    {
        public ItemViewModel()
        {
            _ProductNames = new List<string>();
        }

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { RaisePropertyChanged(ref _Title, value); }
        }

        private string _FirstName;
        public string FirstName
        {
            get { return _FirstName; }
            set { RaisePropertyChanged(ref _FirstName, value); }
        }

        private string _LastName;
        public string LastName
        {
            get { return _LastName; }
            set { RaisePropertyChanged(ref _LastName, value); }
        }

        private string _CompanyName;
        public string CompanyName
        {
            get { return _CompanyName; }
            set { RaisePropertyChanged(ref _CompanyName, value); }
        }

        private string _EmailAddress;
        public string EmailAddress
        {
            get { return _EmailAddress; }
            set { RaisePropertyChanged(ref _EmailAddress, value); }
        }

        private string _Phone;
        public string Phone
        {
            get { return _Phone; }
            set { RaisePropertyChanged(ref _Phone, value); }
        }

        private List<string> _ProductNames;
        public List<string> ProductNames
        {
            get { return _ProductNames; }
            set { RaisePropertyChanged(ref _ProductNames, value); }
        }

        public string ProductNameList
        {
            get { return string.Join(", ", _ProductNames); }
        }

    }
}
