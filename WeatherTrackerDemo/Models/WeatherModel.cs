using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeatherTrackerDemo.Models
{
    public class WeatherModel
    {
        public string City { get; set; }
        public WeatherItem Current { get; set; }
        public List<WeatherItem> Forecast { get; set; }

        public async Task GetWeather(string apikey)
        {
            using (HttpClient client = new HttpClient())
            {
                var url = new Uri($"https://api.weatherapi.com/v1/forecast.json?q={City}&days=3&key={apikey}");
                var res = await client.GetAsync(url);

                if (res.IsSuccessStatusCode)
                {
                    var resString = await res.Content.ReadAsStringAsync();
                    var json = (JObject)JsonConvert.DeserializeObject(resString);
                    this.Current = new WeatherItem(
                        json["current"]["condition"]["icon"].Value<string>(),
                        json["current"]["temp_f"].Value<string>(),
                        json["current"]["feelslike_f"].Value<string>(),
                        0,
                        json["current"]["humidity"].Value<int>(),
                        json["current"]["wind_mph"].Value<string>() + " mph " + json["current"]["wind_dir"].Value<string>(),
                        json["current"]["uv"].Value<int>(),
                        json["current"]["pressure_in"].Value<float>(), "", "",
                        json["current"]["last_updated"].Value<string>());

                    this.Forecast = new List<WeatherItem>();
                    var temp = json["forecast"]["forecastday"][0];
                    for (var i = 0; i < 3; i++)
                    {
                        Forecast.Add(new WeatherItem(
                        json["forecast"]["forecastday"][i]["day"]["condition"]["icon"].Value<string>(),
                        "",
                        "",
                        Math.Max(json["forecast"]["forecastday"][i]["day"]["daily_chance_of_rain"].Value<int>(), json["forecast"]["forecastday"][i]["day"]["daily_chance_of_snow"].Value<int>()),
                        json["forecast"]["forecastday"][i]["day"]["avghumidity"].Value<int>(),
                        json["forecast"]["forecastday"][i]["day"]["maxwind_mph"].Value<string>() + " mph " + json["forecast"]["forecastday"][i]["hour"][0]["wind_dir"].Value<string>(),
                        json["forecast"]["forecastday"][i]["day"]["uv"].Value<int>(),
                        0,
                        json["forecast"]["forecastday"][i]["day"]["mintemp_f"].Value<string>(),
                        json["forecast"]["forecastday"][i]["day"]["maxtemp_f"].Value<string>(),
                        json["forecast"]["forecastday"][i]["date"].Value<string>()));
                    }
                }
            }
        }
    }

    public class WeatherItem
    {
        public string Img { get; set; }
        public string Temperature { get; set; } //Temp - C / F
        public string FeelsLike { get; set; } //Temp - C / F
        public string High {  get; set; }
        public string Low { get; set; }
        public int PrecipitationChance { get; set; }
        public int Humidity { get; set; }
        public string Wind { get; set; } //Direction - Speed
        public int UV { get; set; }
        public float Pressure { get; set; }
        public string DayOfWeek { get; set; }
        public WeatherItem(string img, string temperature, string feelsLike, int precipitationChance, int humidity, string wind, int uV, float pressure, string low, string high, string date)
        {
            Img = img;
            Temperature = temperature + "°F";
            FeelsLike = feelsLike + "°F";
            PrecipitationChance = precipitationChance;
            Humidity = humidity;
            Wind = wind;
            UV = uV;
            Pressure = pressure;
            Low = low + "°F";
            High = high + "°F";
            DayOfWeek = DateTime.Parse(date).DayOfWeek.ToString();
        }
    }
}
