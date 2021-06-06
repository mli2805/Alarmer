using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using AlarmerTest1.DomainModel;
using Caliburn.Micro;

namespace AlarmerTest1.ViewModels
{
  class AllAlarmsViewModel : Screen
  {
    public ObservableCollection<Alarm> AlarmsList { get; set; }

    public AllAlarmsViewModel(ObservableCollection<Alarm> alarms)
    {
      AlarmsList = alarms;

      var view = CollectionViewSource.GetDefaultView(AlarmsList);
      view.SortDescriptions.Add(new SortDescription("RemindAlarmTime", ListSortDirection.Ascending));
    }

    protected override void OnViewLoaded(object view)
    {
      DisplayName = "All alarms";
    }


  }
}
