using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactAnalyzer
{
    public class ImpactAnalysisParams
    {
        public TimeSpan MaxActivityPeriod { get; set; }
        public int HeaderLines { get; set; }
        public bool TimestampsAreValid { get; set; }

        public int AverageHalfBase { get; set; }

        public List<ActivityDefinition> ActivityDefinitionList { get; set; }
        public static ImpactAnalysisParams GetDefaultImpactAnalysisParams()
        {
            ImpactAnalysisParams Params = new ImpactAnalysisParams();

            Params.ActivityDefinitionList = new List<ActivityDefinition>();
            Params.ActivityDefinitionList.Add(new ActivityDefinition(1.5, 3, "Walking"));
            Params.ActivityDefinitionList.Add(new ActivityDefinition(3, 5, "Marching"));
            Params.MaxActivityPeriod = new TimeSpan(0, 2, 0); // 2 minutes
            Params.HeaderLines = 11;
            Params.TimestampsAreValid = true;
            Params.AverageHalfBase = 10;

            return Params;
        }
    }
}
