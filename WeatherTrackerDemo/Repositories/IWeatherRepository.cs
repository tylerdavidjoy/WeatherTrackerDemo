namespace WeatherTrackerDemo.Repositories
{
    public interface IWeatherRepository
    {
        Task WakeDatabase();
        Task<string> GetLocationKey(string location);
    }
}
