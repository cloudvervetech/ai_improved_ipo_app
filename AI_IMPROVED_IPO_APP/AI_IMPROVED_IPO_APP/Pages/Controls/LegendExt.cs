using Syncfusion.Maui.Toolkit.Charts;

namespace AI_IMPROVED_IPO_APP.Pages.Controls
{
    public class LegendExt : ChartLegend
    {
        protected override double GetMaximumSizeCoefficient()
        {
            return 0.5;
        }
    }
}
