using System;
using System.Collections.Generic;
using System.Linq;
using AlarmerTest1.DomainModel;
using Caliburn.Micro;

namespace AlarmerTest1.ViewModels
{
  public class AddOrEditAlarmViewModel : Screen
  {
    public bool ResultFlag { get; set; }
    public Alarm AlarmInWork { get; set; }
    public string AlarmTime { get; set; }

    public static List<RepetitionPeriods> PeriodList { get; set; }

    public AddOrEditAlarmViewModel(Alarm alarmInWork)
    {
      AlarmInWork = alarmInWork;
      PeriodList = Enum.GetValues(typeof(RepetitionPeriods)).OfType<RepetitionPeriods>().ToList();
      AlarmTime = String.Format("{0}:{1:00}",DateTime.Now.Hour,DateTime.Now.Minute+1);
      ResultFlag = false;
    }

    public void SaveNewAlarm()
    {
      ResultFlag = true;

      DateTime timeTime;
      try
      {
        timeTime  = Convert.ToDateTime(AlarmTime);
      }
      catch (Exception)
      {
        timeTime = new DateTime(1, 1, 1, DateTime.Now.Hour, DateTime.Now.Minute + 1, 0);
      }
      AlarmInWork.RemindAlarmTime = AlarmInWork.BasicAlarmTime.Date.AddHours(timeTime.Hour).AddMinutes(timeTime.Minute) ;
      TryClose();
    }

  }
}
