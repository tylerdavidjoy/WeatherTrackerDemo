using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeatherTrackerDemo.Models
{
    public class WeatherModel
    {
        public string City { get; set; }
        public List<WeatherItem> TwelveHourForecast { get; set; }
        public List<WeatherItem> FiveDayForecast { get; set; }

        public async Task GetWeather(string apikey)
        {
            TwelveHourForecast = new List<WeatherItem>();
            FiveDayForecast = new List<WeatherItem>();

            using (HttpClient client = new HttpClient())
            {
                var locationkey = 350143;
                //12-Hour Forecast
                var url = new Uri($"http://dataservice.accuweather.com/forecasts/v1/hourly/12hour/{locationkey}?apikey={apikey}&details=true");
                var res = await client.GetAsync(url);

                if (res.IsSuccessStatusCode)
                {
                    var resString = await res.Content.ReadAsStringAsync();
                    var json = (JArray)JsonConvert.DeserializeObject(resString);

                    foreach (var hour in json)
                    {
                        TwelveHourForecast.Add(new WeatherItem(
                            hour["WeatherIcon"].Value<string>(),
                            "TEST",
                            hour["Temperature"]["Value"].Value<float>(),
                            hour["RealFeelTemperature"]["Value"].Value<float>(),
                            hour["PrecipitationProbability"].Value<int>(),
                            hour["RelativeHumidity"].Value<int>(),
                            hour["Wind"]["Speed"]["Value"].Value<string>() + " MPH " + hour["Wind"]["Direction"]["Localized"].Value<string>(),
                            hour["UVIndexText"].Value<string>(),
                            0,
                            0,
                            hour["DateTime"].Value<DateTime>()
                            ));
                    }

                    //5 Day Forecast
                    url = new Uri($"http://dataservice.accuweather.com/forecasts/v1/daily/5day/{locationkey}?apikey={apikey}&details=true");
                    res = await client.GetAsync(url);

                    if(res.IsSuccessStatusCode)
                    {
                        resString = await res.Content.ReadAsStringAsync();
                        var json1 = (JObject)JsonConvert.DeserializeObject(resString);

                        foreach(var day in json1["DailyForecasts"])
                        {
                            FiveDayForecast.Add(new WeatherItem(
                                day["Day"]["Icon"].Value<string>(),
                                "TEST",
                                0,
                                0,
                                Math.Max(day["Day"]["PrecipitationProbability"].Value<int>(), day["Night"]["PrecipitationProbability"].Value<int>()),
                                day["Day"]["RelativeHumidity"]["Maximum"].Value<int>(),
                                day["Day"]["Wind"]["Speed"]["Value"].Value<string>() + " MPH " + day["Day"]["Wind"]["Direction"]["Localized"].Value<string>(),
                                day["AirAndPollen"][5]["Category"].Value<string>(),
                                day["Temperature"]["Minimum"]["Value"].Value<float>(),
                                day["Temperature"]["Maximum"]["Value"].Value<float>(),
                                day["Date"].Value<DateTime>()));
                        }
                    }
                }
                else
                {
                    GetWeatherTest();
                }
            }
        }

        public async Task GetWeatherTest()
        {
            TwelveHourForecast = new List<WeatherItem>();
            FiveDayForecast = new List<WeatherItem>();

            var time = DateTime.Now;
            for(var i = 0; i < 12; i++)
            {
                TwelveHourForecast.Add(new WeatherItem("1", "Sunny", 75, 80, 10, 70, "20 mph SSE", "High", 60f, 85f, time));
                time = time.AddHours(1);
            }

            time = DateTime.Now;
            for (var i = 0; i < 5; i++)
            {
                FiveDayForecast.Add(new WeatherItem("1", "Sunny", 0, 0, 40, 70, "20 mph SSE", "High", 60f, 85f, time));
                time = time.AddDays(1);
            }
        }
    }

    public class WeatherItem
    {
        public string Img { get; set; }
        public string Description { get; set; } //Weather type
        public float Temperature { get; set; } //Temp - C / F
        public float FeelsLike { get; set; } //Temp - C / F
        public float High {  get; set; }
        public float Low { get; set; }
        public int PrecipitationChance { get; set; }
        public int Humidity { get; set; }
        public string Wind { get; set; } //Direction - Speed
        public string UV { get; set; }
        public DateTime Timestamp { get; set; }
        public WeatherItem(string img, string description, float temperature, float feelsLike, int precipitationChance, int humidity, string wind, string uV, float low, float high, DateTime date)
        {
            Img = img;
            Description = description;
            Temperature = temperature;
            FeelsLike = feelsLike;
            PrecipitationChance = precipitationChance;
            Humidity = humidity;
            Wind = wind;
            UV = uV;
            Low = low;
            High = high;
            Timestamp = date;
        }
    }
}
