using System;

namespace AlarmerTest1.DomainModel
{
  public class Alarm
  {
    public DateTime BasicAlarmTime { get; set; }            // 17/01/2014 9:00             15/02/2014 10:00         5/03/2013 9:00 

    public DateTime RemindAlarmTime { get; set; }           // 17/01/2014 9:00             25/02/2014 10:00         5/03/2013 9:00
    public TimeSpan RemindDelay { get; set; }               // 10 min (default)            1440 min (1 day)         60 min

    public bool IsRegularEvent { get; set; }                // true                        false                    yes
    public RepetitionPeriods Period { get; set; }          // 1 year                      0                        1 year

    public string Subject { get; set; }                     // Have you got a gift         Boris' bycicle           March, 8th 
                                                            // for 24/01?

    public Alarm()
    {
      BasicAlarmTime = DateTime.Now;
      RemindDelay = new TimeSpan(0,10,0);
      RemindAlarmTime = BasicAlarmTime;

      IsRegularEvent = false;
      Period = RepetitionPeriods.EveryYear;

      Subject = "";
    }

    public Alarm(string str)
    {
      var fields = str.Split(';');
      BasicAlarmTime = Convert.ToDateTime(fields[0]);
      RemindAlarmTime = Convert.ToDateTime(fields[1]);
      RemindDelay = String2Timespan(fields[2]); 
      IsRegularEvent = Convert.ToBoolean(fields[3]);
      Period = (RepetitionPeriods)Enum.Parse(typeof(RepetitionPeriods), fields[4]);
      Subject = fields[5];
    }

    public TimeSpan String2Timespan(string str)
    {
      int days = 0;
      if (str.Contains("."))
      {
        var parts = str.Split('.');
        days = Convert.ToInt32(parts[0]);
        str = parts[1];
      }
      var fields = str.Split(':');
      return new TimeSpan(days, Convert.ToInt32(fields[0]),Convert.ToInt32(fields[1]),Convert.ToInt32(fields[2]));
    }

    public string ToFile()
    {
      return String.Format("{0};{1};{2};{3};{4};{5}",
        BasicAlarmTime, RemindAlarmTime, RemindDelay, IsRegularEvent, Period, Subject);
    }

    public void SetNextBasicTime()
    {
      if (!IsRegularEvent) return;

      switch (Period)
      {
        case RepetitionPeriods.EveryDay:
          BasicAlarmTime = BasicAlarmTime.AddDays(1);
          break;

        case RepetitionPeriods.EveryWeek:
          BasicAlarmTime = BasicAlarmTime.AddDays(7);
          break;

        case RepetitionPeriods.EveryMonth:
          BasicAlarmTime = BasicAlarmTime.AddMonths(1); 
          break;

        case RepetitionPeriods.EveryYear:
          BasicAlarmTime = BasicAlarmTime.AddYears(1); 
          break;
      }

      RemindAlarmTime = BasicAlarmTime;
    }

  }


}