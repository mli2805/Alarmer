using System;
using System.Collections.Generic;
using System.Windows;
using AlarmerTest1.DomainModel;
using Caliburn.Micro;

namespace AlarmerTest1.ViewModels
{
  class AlarmBaloonViewModel : Screen
  {
    public Alarm CurrentAlarm { get; set; }
    public string YesISeeHint { get { return CurrentAlarm.IsRegularEvent ? String.Format("Регулярное событие\nСейчас отстанет, но будет\nповторяться {0}", CurrentAlarm.Period) : "Реальное удаление"; } }

    public List<string> RemindIn { get; set; }
    public string SelectedRemindIn { get; set; }

    public RemindResult Result { get; set; }

    public AlarmBaloonViewModel(Alarm alarm)
    {
      RemindIn = new List<string>
                   {
                     "1 минуту",
                     "3 минуты",
                     "5 минут",
                     "10 минут",
                     "15 минут",
                     "20 минут",
                     "30 минут",
                     "45 минут",
                     "1 час",
                     "2 часа",
                     "3 часа",
                     "4 часа",
                     "6 часов",
                     "12 часов",
                     "24 часа",
                     "36 часов",
                   };

      CurrentAlarm = alarm;
      SelectedRemindIn = RemindTimeToString(alarm.RemindDelay);
    }

    protected override void OnViewLoaded(object view)
    {
      DisplayName = "Dr-r-r-r !!!!";
    }

    public TimeSpan StringToRemindTime(string st)
    {
      var parts = st.Split(' ');
      return parts[1].StartsWith("час") ? new TimeSpan(Convert.ToInt32(parts[0]),0,0) : new TimeSpan(0,Convert.ToInt32(parts[0]),0);
    }

    public string RemindTimeToString(TimeSpan period)
    {
      foreach (string s in RemindIn)
      {
        if (StringToRemindTime(s) == period) return s;
      }
      return "";
    }

    public void RemindMe()
    {
      CurrentAlarm.RemindDelay = StringToRemindTime(SelectedRemindIn);
      CurrentAlarm.RemindAlarmTime = DateTime.Now + CurrentAlarm.RemindDelay;
      Result = RemindResult.Ничего;
      TryClose();
    }


    /// <summary>
    /// для регулярного события: нарастить времена и закрыть
    /// для нерегулярного - удалить с подтверждением
    /// </summary>
    public void CloseBaloon()
    {
      if (CurrentAlarm.IsRegularEvent)
      {
        CurrentAlarm.SetNextBasicTime();
        Result = RemindResult.Ничего;
        TryClose();
      }
      else
      {
        if (MessageBox.Show("НЕрегулярное событие. При закрытии будет удалено!", "", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
        {
          Result = RemindResult.Удалить;
          TryClose();
        }
      }
    }

    public void EditAlarm()
    {
      Result = RemindResult.Редактировать;
      TryClose();
    }

    public void DeleteAlarm()
    {
      Result = RemindResult.Удалить;
      TryClose();
    }


    public override void CanClose(Action<bool> callback)
    {
      base.CanClose(callback);
      OnClose(EventArgs.Empty);
    }

    public event EventHandler<EventArgs> Close;

    public void OnClose(EventArgs e)
    {
      EventHandler<EventArgs> handler = Close;
      if (handler != null) handler(this, e);
    }

  }
}
