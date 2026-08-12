using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace VCheckViewer_Others.ViewModels.Windows
{
    public class MainViewModel
    {
        string _backButtonText;
        public string BackButtonText {  get { return _backButtonText; } set {  _backButtonText = value; } }

        string _origin;

        public string Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        UserModel _currentUser;
        public UserModel CurrentUsers
        {
            get { return _currentUser; }
            set { _currentUser = value; }
        }

        int _currentUserIndexStart;
        public int CurrentUserIndexStart
        {
            get { return _currentUserIndexStart; }
            set { _currentUserIndexStart = value; }
        }

        UserModel _user;
        public UserModel Users
        {
            get { return _user; }
            set { _user = value; }
        }

        List<ConfigurationModel> _configurationModel;
        public List<ConfigurationModel> ConfigurationModel
        {
            get { return _configurationModel; }
            set { _configurationModel = value; }
        }

        NotificationSearch _searchModel;
        public NotificationSearch SearchModel
        {
            get { return _searchModel; }
            set { _searchModel = value; }
        }

        ObservableCollection<ComboBoxItem> _cbTitle;
        public ObservableCollection<ComboBoxItem> cbTitle
        {
            get { return _cbTitle; }
            set { _cbTitle = value; }
        }

        ObservableCollection<ComboBoxItem> _cbGender;
        public ObservableCollection<ComboBoxItem> cbGender
        {
            get { return _cbGender; }
            set { _cbGender = value; }
        }

        ObservableCollection<ComboBoxItem> _cbRoles;
        public ObservableCollection<ComboBoxItem> cbRoles
        {
            get { return _cbRoles; }
            set { _cbRoles = value; }
        }

        ObservableCollection<ComboBoxItem> _cbStatus;
        public ObservableCollection<ComboBoxItem> cbStatus
        {
            get { return _cbStatus; }
            set { _cbStatus = value; }
        }

        ObservableCollection<ComboBoxItem> _cbSort;
        public ObservableCollection<ComboBoxItem> cbSort
        {
            get { return _cbSort; }
            set { _cbSort = value; }
        }

        ObservableCollection<ComboBoxItem> _cbCountryPhoneNum;
        public ObservableCollection<ComboBoxItem> cbCountryPhoneNum
        {
            get { return _cbCountryPhoneNum; }
            set { _cbCountryPhoneNum = value; }
        }

        ObservableCollection<ComboBoxItem> _cbCountryPhoneNumSearch;
        public ObservableCollection<ComboBoxItem> cbCountryPhoneNumSearch
        {
            get { return _cbCountryPhoneNumSearch; }
            set { _cbCountryPhoneNumSearch = value; }
        }

        ObservableCollection<ComboBoxItem> _cbDateFormat;
        public ObservableCollection<ComboBoxItem> cbDateFormat
        {
            get { return _cbDateFormat; }
            set { _cbDateFormat = value; }
        }

        ObservableCollection<ComboBoxItem> _cbConnectionType;
        public ObservableCollection<ComboBoxItem> cbConnectionType
        {
            get { return _cbConnectionType; }
            set { _cbConnectionType = value; }
        }

        ComboBoxItem _SelectedcbSort;
        public ComboBoxItem SelectedcbSort
        {
            get { return _SelectedcbSort; }
            set { _SelectedcbSort = value; }
        }

        ComboBoxItem _SelectedcbTitle;
        public ComboBoxItem SelectedcbTitle
        {
            get { return _SelectedcbTitle; }
            set { _SelectedcbTitle = value; }
        }

        ComboBoxItem _SelectedcbGender;
        public ComboBoxItem SelectedcbGender
        {
            get { return _SelectedcbGender; }
            set { _SelectedcbGender = value; }
        }

        ComboBoxItem _SelectedcbRoles;
        public ComboBoxItem SelectedcbRoles
        {
            get { return _SelectedcbRoles; }
            set { _SelectedcbRoles = value; }
        }

        ComboBoxItem _SelectedcbStatus;
        public ComboBoxItem SelectedcbStatus
        {
            get { return _SelectedcbStatus; }
            set { _SelectedcbStatus = value; }
        }

        ComboBoxItem _SelectedcbCountryPhoneNum;
        public ComboBoxItem SelectedcbCountryPhoneNum
        {
            get { return _SelectedcbCountryPhoneNum; }
            set { _SelectedcbCountryPhoneNum = value; }
        }

        DeviceModel _deviceModel;
        public DeviceModel DeviceModel
        {
            get { 
                return _deviceModel; 
            }
            set { 
                _deviceModel = value; 
            }
        }

        String _oldDeviceName;
        public String OldDeviceName
        {
            get
            {
                return _oldDeviceName;
            }
            set
            {
                _oldDeviceName = value;
            }
        }

        String _ScheduleUniqueID;
        public String ScheduleUniqueID
        {
            get
            {
                return _ScheduleUniqueID;
            }
            set
            {
                _ScheduleUniqueID = value;
            }
        }

        String _TestResultID;
        public String TestResultID
        {
            get
            {
                return _TestResultID;
            }
            set
            {
                _TestResultID = value;
            }
        }
    }
}
