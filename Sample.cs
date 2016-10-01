using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalyzer
{
    class Sample
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
        public double AbsValue { get; set; }
        public bool IsPeakOrValley { get; set; }

        public double Average { get; set; }

        public Sample(DateTime time, float value)
        {
            Time = time;
            Value = value;
            AbsValue = Math.Abs(Value);
            IsPeakOrValley = false;
        }
    }

    public class ActivityDefinition
    {
        public double ImpactLowWaterMark { get; set; }

        // Impact high water mark for this activity
        public double ImpactHighWaterMark { get; set; }

        public string Name { get; set; }

        public ActivityDefinition(double impactLowWaterMark, double impactHighWaterMark, string name)
        {
            ImpactLowWaterMark  = impactLowWaterMark;
            ImpactHighWaterMark = impactHighWaterMark;
            Name                = name;
        }
    }

    class Activity
    {
        public ActivityDefinition Definition { get; set; }

        public List<Sample> SampleList { get; set; }

        // The time difference between the peaks in the activity
        public List<TimeSpan> TimeDifference { get; set; }

        // Average time difference
        public TimeSpan AverageTimeDifference { get; set; }

        // Unique identifier for this activity
        public int ActivityId { get; set; }

        public Activity(List<Sample> sampleList, ActivityDefinition definition, int id)
        {
            SampleList = sampleList;
            Definition = definition;
            ActivityId = id;
            ComputeSampleTimeDifference();
        }

        void ComputeSampleTimeDifference()
        {
            if (SampleList.Count > 0)
            {
                TimeDifference = new List<TimeSpan>(SampleList.Count - 1);
                TimeSpan TotalTimeDifference = new TimeSpan(0);

                for (int i = 1; i < SampleList.Count; i++)
                {
                    TimeDifference.Add(SampleList[i].Time - SampleList[i - 1].Time);
                    TotalTimeDifference += TimeDifference[i - 1];
                }

                AverageTimeDifference = new TimeSpan((long)(TotalTimeDifference.TotalMilliseconds * 10000 / (SampleList.Count - 1)));
            }
            else
            {
                TimeDifference = new List<TimeSpan>(0);
                AverageTimeDifference = new TimeSpan(0);
            }
        }

        public Int32 FindBreakInActivity(ImpactAnalysisParams Params)
        {
            return TimeDifference.FindIndex(timedifference => timedifference >= Params.MaxActivityPeriod);
        }

        public Activity BreakOff(Int32 index)
        {
            // The index is an index into the TimeDifference list
            // This means that we should break off index+1 elements from the head of the activity list

            Activity BreakOffActivity = new Activity(SampleList.GetRange(0, index + 1), Definition, ActivityId);
            SampleList.RemoveRange(0, index + 1);
            ComputeSampleTimeDifference();

            return BreakOffActivity;
        }

        public DateTime ActivityStartTime()
        {
            return (SampleList.Count > 0) ? SampleList[0].Time : new DateTime(0, DateTimeKind.Utc);
        }

        public DateTime ActivityEndTime()
        {
            return (SampleList.Count > 0) ? SampleList[SampleList.Count - 1].Time : new DateTime(0, DateTimeKind.Utc);
        }
    }

    class ActivityData
    {
        private ImpactAnalysisParams _Params;
        public ImpactAnalysisParams Params 
        { 
            get
            {
                return _Params;
            }
        }

        private List<Activity> _ActivityList;
        public List<Activity> ActivityList
        {
            get
            {
                return _ActivityList;
            }
        }

        public ActivityData(List<Activity> activityList, ImpactAnalysisParams param)
        {
            _Params = param;
            _ActivityList = activityList;
        }
    }
}
