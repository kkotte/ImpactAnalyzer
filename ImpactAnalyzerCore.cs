using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace ImpactAnalyzer
{
    class ImpactAnalyzerCore
    {
        public static async Task<List<Sample>> GetSampleListFromFileAsync(ImpactAnalysisParams Params, string filename)
        {
            List<Sample> SampleList = new List<Sample>();

            using (StreamReader reader = new StreamReader(filename))
            {
                // Skip header lines
                for (int i = 0; i < Params.HeaderLines; i++)
                {
                    await reader.ReadLineAsync();
                }

                // Parse all the following lines
                // Format of the lines is:
                // %d,%f,%f,%f
                string line;
                long timeinterval = 0;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string[] fields = line.Split(',');

                    // Let's pick the first column
                    if (Params.TimestampsAreValid)
                    {
                        // Format of timestamp appears to be
                        // mm/dd/yyyy HH:MM:SS.mmm

                        string TimeStampFormat = "M/d/yyyy HH:mm:ss.FFF";

                        SampleList.Add(new Sample(DateTime.ParseExact(fields[0], TimeStampFormat, CultureInfo.InvariantCulture), (float)Convert.ToDouble(fields[1])));
                    }
                    else
                    {
                        SampleList.Add(new Sample(new DateTime(timeinterval++ * 10000000 / 60, DateTimeKind.Utc), (float)Convert.ToDouble(fields[1])));
                    }
                }
            }
            
            return SampleList;
        }

        public static List<Sample> GetSampleListFromFile(ImpactAnalysisParams Params, string filename)
        {
            List<Sample> SampleList = new List<Sample>();

            Console.WriteLine("Parsing {0}...", filename);
            using (StreamReader reader = new StreamReader(filename))
            {
                // Skip header lines
                for (int i = 0; i < Params.HeaderLines; i++)
                {
                    reader.ReadLine();
                }

                // Parse all the following lines
                // Format of the lines is:
                // %d,%f,%f,%f
                string line;
                long timeinterval = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] fields = line.Split(',');

                    // Let's pick the first column
                    if (Params.TimestampsAreValid)
                    {
                        // Format of timestamp appears to be
                        // mm/dd/yyyy HH:MM:SS.mmm

                        string TimeStampFormat = "M/d/yyyy HH:mm:ss.FFF";

                        SampleList.Add(new Sample(DateTime.ParseExact(fields[0], TimeStampFormat, CultureInfo.InvariantCulture), (float)Convert.ToDouble(fields[1])));
                    }
                    else
                    {
                        SampleList.Add(new Sample(new DateTime(timeinterval++ * 10000000 / 60, DateTimeKind.Utc), (float)Convert.ToDouble(fields[1])));
                    }
                }
                Console.WriteLine("Parsing complete.");

                Console.WriteLine("Read {0} records", SampleList.Count);
            }

            ComputeMovingAverage(SampleList, Params.AverageHalfBase);
            MarkPeaksAndValleys(SampleList);

            return SampleList;
        }

        static void MarkPeaksAndValleys(List<Sample> SampleList)
        {
            for (int i = 1; i < SampleList.Count - 1; i++)
            {
                SampleList[i].IsPeakOrValley = (SampleList[i - 1].Value < SampleList[i].Value) && (SampleList[i].Value > SampleList[i + 1].Value) // Peak
                                               ||
                                               (SampleList[i - 1].Value > SampleList[i].Value) && (SampleList[i].Value < SampleList[i + 1].Value); // Valley
            }
        }

        static void ComputeMovingAverage(List<Sample> SampleList, int AverageHalfBase)
        {
            for (int i = 0; i < SampleList.Count; i++)
            {
                // Inefficient, I know....
                int Start = Math.Max(0, i - AverageHalfBase);
                int NumSamples = Math.Min(i + AverageHalfBase, SampleList.Count - 1) - Start + 1;
                SampleList[i].Average = SampleList.GetRange(Start, NumSamples).Sum(sample => sample.Value) / NumSamples;
            }
        }

        public static List<Activity> GetActivityList(ImpactAnalysisParams Params, List<Sample> SampleList)
        {
            List<Activity> ActivityList = ClassifySamplesIntoActivities(Params, SampleList);
            ActivityList = ActivityTimeAnalysis(Params, ActivityList);
            return ActivityList;
        }

        static List<Activity> ClassifySamplesIntoActivities(ImpactAnalysisParams Params, List<Sample> SampleList)
        {
            List<Activity> ActivityList;

            Console.WriteLine("Classifying samples into activities.");
            ActivityList = new List<Activity>(Params.ActivityDefinitionList.Count - 1);
            int i = 0;
            foreach (ActivityDefinition activityDefinition in Params.ActivityDefinitionList)
            {
                ActivityList.Add(new Activity(SampleList.FindAll((Sample sample) => (sample.IsPeakOrValley && sample.AbsValue >= activityDefinition.ImpactLowWaterMark && sample.AbsValue < activityDefinition.ImpactHighWaterMark)), new ActivityDefinition(activityDefinition.ImpactLowWaterMark, activityDefinition.ImpactHighWaterMark, activityDefinition.Name), i));
                Console.WriteLine("Number of samples in activity marked by impact zone [{0},{1}) is {2}", activityDefinition.ImpactLowWaterMark, activityDefinition.ImpactHighWaterMark, ActivityList[i].SampleList.Count);
                i++;
            }

            return ActivityList;
        }

        static List<Activity> ActivityTimeAnalysis(ImpactAnalysisParams Params, List<Activity> ActivityList)
        {
            List<Activity> TimeAnalyzedActivityList = new List<Activity>();

            foreach (Activity activity in ActivityList)
            {
                Int32 index;
                // Identify discrete durations when the activity was being performed
                while ((index = activity.FindBreakInActivity(Params)) != -1)
                {
                    // Break this off into a separate activity
                    TimeAnalyzedActivityList.Add(activity.BreakOff(index));
                }

                if (activity.SampleList.Count > 0)
                {
                    TimeAnalyzedActivityList.Add(activity);
                }
            }

            return TimeAnalyzedActivityList;
        }
    }

}

