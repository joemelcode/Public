using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace Knownrandom
{
    public static class HttpTrigger1
    {
        private static string connectionString = "Server=tcp:knownrandoms.database.windows.net,1433;Initial Catalog=KnownRandoms;Persist Security Info=False;User ID=Joemel;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        [FunctionName("HttpTrigger1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get query parameter or request body for 'name'
            string name = req.Query["name"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // Prepare response message
            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            // If the 'name' parameter is provided, query the database
            if (!string.IsNullOrEmpty(name))
            {
                try
                {
                    // Connect to the database and fetch data
                    var matches = await GetMatchesFromDatabase(name);

                    // Return the results as JSON
                    return new OkObjectResult(matches);
                }
                catch (Exception ex)
                {
                    log.LogError($"An error occurred while querying the database: {ex.Message}");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }

            return new OkObjectResult(responseMessage);
        }

        private static async Task<List<Match>> GetMatchesFromDatabase(string name)
        {
            var matches = new List<Match>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var text = "SELECT * FROM Matches WHERE Name = @Name";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        matches.Add(new Match
                        {
                            MatchID = (int)reader["MatchID"],
                            UserID = (int)reader["UserID"],
                            GameMode = (string)reader["GameMode"],
                            Position = (string)reader["Position"],
                            Timestamp = (DateTime)reader["Timestamp"]
                        });
                    }
                }
            }

            return matches;
        }

        // Define the Match class to represent database records
        public class Match
        {
            public int MatchID { get; set; }
            public int UserID { get; set; }
            public string GameMode { get; set; }
            public string Position { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
