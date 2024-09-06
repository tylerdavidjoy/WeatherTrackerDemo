using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Web;

namespace WeatherTrackerDemo.Repositories
{
    public class WeatherRepository : IWeatherRepository
    {
        private IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _apiKey;
        public WeatherRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetValue<string>("SQL:ConnectionString");
            _apiKey = _configuration.GetValue<string>("Weather:APIKEY");
        }

        public async Task<string> GetLocationKey(string location)
        {
            string locationKey = "";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                //See if it is cached in our database and is valid
                var sql = @$"SELECT TOP(1) LocationKey
                              FROM [dbo].[LocationKeys]
                              WHERE City LIKE '%{location}%'
                              AND Valid > '{DateTime.Now}';";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locationKey = reader.GetString(0);
                        }
                    }
                }

                if (String.IsNullOrEmpty(locationKey)) //Key was not cached or valid, Get from API and cache it
                {
                    using (HttpClient client = new HttpClient())
                    {
                        //12-Hour Forecast                        
                        var url = new Uri($"http://dataservice.accuweather.com/locations/v1/cities/search?apikey={_apiKey}&q={location}");
                        var res = await client.GetAsync(url);

                        if (res.IsSuccessStatusCode)
                        {
                            var resString = await res.Content.ReadAsStringAsync();
                            var json = ((JArray)JsonConvert.DeserializeObject(resString))[0];
                            locationKey = json["Key"].Value<string>();
                        }
                        else //Used up API limit for today, use fallback default
                        {
                            return "";
                        }
                    }

                    //Cache value in database
                    sql = @$"BEGIN TRY     
                                INSERT INTO dbo.LocationKeys VALUES
                                    (
		                                '{location}',
		                                '{locationKey}',
		                                '{DateTime.Now.AddMonths(1)}'
		                            )
                            END TRY     
                            BEGIN CATCH     
                                -- ignore duplicate key errors, throw the rest.
                                IF ERROR_NUMBER() IN (2601, 2627) 
                                    UPDATE dbo.LocationKeys
                                        SET LocationKey = '{locationKey}',
	                                    Valid = '{DateTime.Now.AddMonths(1)}'
                                        WHERE City = '{location}';     
                            END CATCH";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        try
                        {
                            if (await command.ExecuteNonQueryAsync() == 0)
                            {
                                //Error inserting into the database
                                throw new Exception("Failed to cache location key");
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex.ToString());
                            //No big issue if it fails to cache, there is a fallback if api limit is reached for the weather api
                        }
                    }
                }
            }
            return locationKey;
        }
    }
}
