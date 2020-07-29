namespace raidTimelineLogic
{
	public interface ITimelineCreator
	{
		void CreateTimelineFileFromDisk(string path, string outputFileName);
		void CreateTimelineFileFromWeb(string path, string outputFileName, string token, int numberOfLogs);
	}
}