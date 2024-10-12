using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

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
            _connectionString = _configuration.GetValue<string>("SQL-CONNECTION-STRING");
            _apiKey = _configuration.GetValue<string>("WEATHER-API-KEY");
        }
        //Database is serverless, as stated in Azure documentation, https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview?view=azuresql&tabs=general-purpose#connectivity
        //The database is paused after not being used for 10 minutes.
        //Database takes ~1 minute to wake, this will wake the database when someone visits the site.
        public async Task WakeDatabase()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
            }
        }

        public async Task<string> GetLocationKey(string location)
        {
            var options = new SqlRetryLogicOption()
            {
                // Tries 5 times before throwing an exception
                NumberOfTries = 5,
                // Preferred gap time to delay before retry
                DeltaTime = TimeSpan.FromSeconds(1),
                // Maximum gap time for each delay time before retry
                MaxTimeInterval = TimeSpan.FromSeconds(20)
                
            };
            SqlRetryLogicBaseProvider provider = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(options);

            string locationKey = "";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.RetryLogicProvider = provider;

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
