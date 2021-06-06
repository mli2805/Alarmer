using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Threading;
using AlarmerTest1.DomainModel;
using AlarmerTest1.Properties;
using Caliburn.Micro;
using System.ComponentModel.Composition;

namespace AlarmerTest1.ViewModels
{
    [Export(typeof(IShell))]
    public class ShellViewModel : Screen, IShell
    {
        [Import]
        public IWindowManager WindowManager { get; set; }

        public static Encoding Encoding1251 = Encoding.GetEncoding(1251);
        private readonly string _homeFile;
        private readonly string _officeFile;
        private readonly string _dataForHome;
        private readonly string _dataForOffice;
        private readonly string _permissionForHome;
        private readonly string _permissionForOffice;
        private readonly bool _isAtHome;

        private string _eventsCount;
        public string EventsCount
        {
            get { return _eventsCount; }
            set
            {
                if (value == _eventsCount) return;
                _eventsCount = value;
                NotifyOfPropertyChange(() => EventsCount);
            }
        }

        public ObservableCollection<Alarm> AlarmList { get; set; }

        private readonly Dictionary<Alarm, AlarmBaloonViewModel> _shownBaloons;
        public string WorkingDirectory { get; set; }

        public ShellViewModel()
        {
            _isAtHome = Environment.MachineName != "opx-lmarholin2".ToUpper();
            var dropboxPath = _isAtHome ? Settings.Default.HomeAlarmListPath : Settings.Default.OfficeAlarmListPath;
            _homeFile = Path.Combine(dropboxPath, Settings.Default.HomeListName);
            _officeFile = Path.Combine(dropboxPath, Settings.Default.OfficeListName);
            _dataForHome = Path.Combine(dropboxPath, Settings.Default.DataForHome);
            _dataForOffice = Path.Combine(dropboxPath, Settings.Default.DataForOffice);
            _permissionForHome = Path.Combine(dropboxPath, Settings.Default.PermissionForHome);
            _permissionForOffice = Path.Combine(dropboxPath, Settings.Default.PermissionForOffice);



            _shownBaloons = new Dictionary<Alarm, AlarmBaloonViewModel>();

            AlarmList = _isAtHome ? ReadAlarmsFile(_homeFile) : ReadAlarmsFile(_officeFile);

            WorkingDirectory = Directory.GetCurrentDirectory();
            EventsCount = $"Событий: {AlarmList.Count}";

            var mainTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            mainTimer.Tick += MainTimerOnTick;
            mainTimer.Start();

            var checkRemoteJobsTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 30) };
            checkRemoteJobsTimer.Tick += CheckRemoteJobsTimerOnTick;
            checkRemoteJobsTimer.Start();
        }

        private void UpdateAlarmList(string fileFromRemote)
        {
            var tempAlarmList = ReadAlarmsFile(fileFromRemote);
            foreach (Alarm alarm in tempAlarmList)
            {
                AlarmList.Add(alarm);
                string fileName = _isAtHome ? _homeFile : _officeFile;
                WriteAlarmList(AlarmList, fileName);
            }
        }

        private void CheckIncomeJobs()
        {
            var pattern = _isAtHome ? Settings.Default.PermissionForHome : Settings.Default.PermissionForOffice;
            var path = _isAtHome ? Settings.Default.HomeAlarmListPath : Settings.Default.OfficeAlarmListPath;
            pattern += "*";
            var files = Directory.GetFiles(path, pattern);
            foreach (string file in files)
            {
                var guid = Path.GetExtension(file);
                var dataFile = _isAtHome ? _dataForHome : _dataForOffice;
                dataFile += guid.Substring(1);
                if (File.Exists(dataFile))
                {
                    UpdateAlarmList(dataFile);
                    EventsCount = String.Format("Событий: {0}", AlarmList.Count);
                    File.Delete(dataFile);
                }
            }
        }

        private void CheckOutcomePermissions()
        {
            var pattern = _isAtHome ? Settings.Default.PermissionForOffice : Settings.Default.PermissionForHome;
            var path = _isAtHome ? Settings.Default.HomeAlarmListPath : Settings.Default.OfficeAlarmListPath;
            pattern += "*";
            var files = Directory.GetFiles(path, pattern);
            foreach (string file in files)
            {
                var guid = Path.GetExtension(file);
                var dataFile = _isAtHome ? _dataForOffice : _dataForHome;
                dataFile += guid.Substring(1);
                if (!File.Exists(dataFile)) File.Delete(file);
            }
        }

        private void CheckRemoteJobsTimerOnTick(object sender, EventArgs eventArgs)
        {
            CheckIncomeJobs();
            CheckOutcomePermissions();
        }

        void MainTimerOnTick(object sender, EventArgs e)
        {
            foreach (var alarm in AlarmList)
            {
                if (DateTime.Now < alarm.RemindAlarmTime) continue;
                AlarmBaloonViewModel baloon;
                if (_shownBaloons.TryGetValue(alarm, out baloon))
                    if (baloon.IsActive) continue; else _shownBaloons.Remove(alarm);
                ShowAlarmBaloon(alarm);
            }
        }

        public override void CanClose(Action<bool> callback)
        {
            foreach (var pair in _shownBaloons)
            {
                pair.Value.TryClose();
            }
            callback(true);
        }

        #region read/write alarm list

        public ObservableCollection<Alarm> ReadAlarmsFile(string alarmsFile)
        {
            var alarmList = new ObservableCollection<Alarm>();

            var content = File.ReadAllLines(alarmsFile, Encoding1251).Where(s => !string.IsNullOrWhiteSpace(s));
            foreach (var line in content)
            {
                alarmList.Add(new Alarm(line));
            }
            return alarmList;
        }

        public void WriteAlarmList(ObservableCollection<Alarm> alarmList, string alarmsFile)
        {
            var content = alarmList.Select(alarm => alarm.ToFile()).ToList();
            File.WriteAllLines(alarmsFile, content, Encoding1251);
        }

        #endregion

        #region Показ напоминания - Обработка решения пользователя

        public void ShowAlarmBaloon(Alarm alarm)
        {
            var alarmBaloonViewModel = new AlarmBaloonViewModel(alarm);

            alarmBaloonViewModel.Close += AlarmBaloonViewModelClose;

            _shownBaloons.Add(alarm, alarmBaloonViewModel);
            WindowManager.ShowWindow(alarmBaloonViewModel);

            try
            {
                var player = new SoundPlayer("sound.wav");
                player.Play();
            }
            catch (Exception)
            {
                SystemSounds.Exclamation.Play();
            }
        }

        public void EditAlarm(ref Alarm alarm)
        {
            var alarmInWork = alarm;
            var addAlarmViewModel = new AddOrEditAlarmViewModel(alarmInWork);
            WindowManager.ShowDialog(addAlarmViewModel);
            if (addAlarmViewModel.ResultFlag) alarm = addAlarmViewModel.AlarmInWork;
        }

        void AlarmBaloonViewModelClose(object sender, EventArgs e)
        {
            var baloon = (AlarmBaloonViewModel)sender;

            if (baloon.Result == RemindResult.Ничего)
            {
                if (_isAtHome) WriteAlarmList(AlarmList, _homeFile); else WriteAlarmList(AlarmList, _officeFile);
                return;
            }
            if (baloon.Result == RemindResult.Редактировать)
            {
                var alarmForEditig = baloon.CurrentAlarm;
                EditAlarm(ref alarmForEditig);
                if (_isAtHome) WriteAlarmList(AlarmList, _homeFile); else WriteAlarmList(AlarmList, _officeFile);
                return;
            }
            _shownBaloons.Remove(baloon.CurrentAlarm);
            if (baloon.Result == RemindResult.Удалить)
            {
                AlarmList.Remove(baloon.CurrentAlarm);
                if (_isAtHome) WriteAlarmList(AlarmList, _homeFile); else WriteAlarmList(AlarmList, _officeFile);
                EventsCount = String.Format("Событий: {0}", AlarmList.Count);
            }
        }

        #endregion

        #region ContextMenu: ShowAll - AddNew - Exit

        public void RemoteAdding(Alarm newAlarm)
        {
            var tempAlarmlist = new ObservableCollection<Alarm>();
            tempAlarmlist.Add(newAlarm);
            string dataForRemote = _isAtHome ? _dataForOffice : _dataForHome;
            string permissionForRemote = _isAtHome ? _permissionForOffice : _permissionForHome;
            string guidForRemote = Guid.NewGuid().ToString();
            dataForRemote += guidForRemote;
            permissionForRemote += guidForRemote;
            WriteAlarmList(tempAlarmlist, dataForRemote);
            File.WriteAllText(permissionForRemote, @"This is permission", Encoding1251);
        }

        public void LocalAdding(Alarm newAlarm)
        {
            AlarmList.Add(newAlarm);
            string fileName = _isAtHome ? _homeFile : _officeFile;
            WriteAlarmList(AlarmList, fileName);
            EventsCount = String.Format("Событий: {0}", AlarmList.Count);
        }

        public void AddNewAlarm(bool isAtHome)
        {
            var alarmInWork = new Alarm();
            var addAlarmViewModel = new AddOrEditAlarmViewModel(alarmInWork);
            WindowManager.ShowDialog(addAlarmViewModel);

            if (!addAlarmViewModel.ResultFlag) return;

            // logical xor
            if (_isAtHome ^ isAtHome)
                RemoteAdding(addAlarmViewModel.AlarmInWork);
            else LocalAdding(addAlarmViewModel.AlarmInWork);
        }

        public void AddNewOffice() { AddNewAlarm(false); }
        public void AddNewHome() { AddNewAlarm(true); }

        public void ShowAllAlarms(ObservableCollection<Alarm> alarmList)
        {
            var allAlarmsViewModel = new AllAlarmsViewModel(alarmList);
            WindowManager.ShowWindow(allAlarmsViewModel);
        }

        public void ShowAllOffice()
        {
            AlarmList = ReadAlarmsFile(_officeFile);
            ShowAllAlarms(AlarmList);
        }
        public void ShowAllHome()
        {
            AlarmList = ReadAlarmsFile(_homeFile);
            ShowAllAlarms(AlarmList);
        }

        public void ExitApplication()
        {
            TryClose();
        }

        #endregion

    }
}