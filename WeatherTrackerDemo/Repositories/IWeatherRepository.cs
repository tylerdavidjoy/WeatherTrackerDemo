namespace WeatherTrackerDemo.Repositories
{
    public interface IWeatherRepository
    {
        Task<string> GetLocationKey(string location);
    }
}
