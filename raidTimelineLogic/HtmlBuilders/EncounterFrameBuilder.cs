using System.Text;
using System.Web;
using raidTimelineLogic.Models;

namespace raidTimelineLogic.HtmlBuilders
{
    public static class EncounterFrameBuilder
    {
        public static StringBuilder BuildEncounterHeader(this StringBuilder stringBuilder, RaidModel raidModel)
        {
	        var encounterTime = raidModel.OccurenceEnd - raidModel.OccurenceStart;
	        
            stringBuilder.Append($@"
				<div class=""content"">
					<a href=""{raidModel.LogUrl}"" target=""_blank"" style=""color: #aaa; text-decoration: none;"">
						<img src=""{raidModel.EncounterIcon}"" alt=""{HttpUtility.HtmlEncode(raidModel.EncounterName)}"" width=""64"" height=""64"" style=""float: right;"">
						<h2>{HttpUtility.HtmlEncode(raidModel.EncounterName)}</h2>
						<p>{raidModel.OccurenceStart.ToLongTimeString()} &rArr; {raidModel.OccurenceEnd.ToLongTimeString()} ({encounterTime.Minutes}m {encounterTime.Seconds}s)
					</a>
					");

            foreach (var value in raidModel.HpLeft)
            {
                stringBuilder.Append($@"
					<div title=""{RaidModel.DoubleAsHtml(value)}% left"" style=""background: rgba(0, 0, 0, 0)
						linear-gradient(to right, red {RaidModel.DoubleAsHtml(value)}%, {RaidModel.DoubleAsHtml(value)}%, green {RaidModel.DoubleAsHtml(100 - value)}%)
						repeat scroll 0% 0%; height: 10px; width: 100%; border-radius: 5px; margin-top: 3px;"">
					</div>");
            }
            
            return stringBuilder;
        }

        public static StringBuilder BuildEncounterFooter(this StringBuilder stringBuilder)
        {
	        stringBuilder.Append($@"
				</div>
			</div>
			");
	        return stringBuilder;
        }
    }
}