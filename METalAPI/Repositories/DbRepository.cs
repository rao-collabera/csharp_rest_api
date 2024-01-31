using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NJsonSchema;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace METalAPI
{
    /// <summary>
    /// Handles DB Calls.
    /// </summary>
    public class DbRepository(IConfiguration Config, IHttpContextAccessor Context, IMemoryCache Cache) : IDbRepository
    {
        private readonly string _controller = Context.HttpContext.Request.RouteValues["controller"].ToString();
        /// <summary>
        /// Returns Requested Record.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="actionID">The action identifier.</param>
        /// <returns></returns>
        public async Task<string> Query(string action, string actionID)
        {
            return await Query("GET", action, actionID);
        }

        /// <summary>
        /// Returns Resultset.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<string> Query(string action, JsonElement payload)
        {
            return await Query("POST", action, "", payload);
        }

        private async Task<string> Query(string actionType, string action, string id, JsonElement payload = default)
        {
            string result; string errorMsg = "";
            string procedure = Config[_controller + actionType + ":" + action];
            if (string.IsNullOrWhiteSpace(procedure)) throw new CustomException(404, $"'{_controller} - {action}' Action Not Found");
            string connectionString = Config[_controller + "Connection"]; //Get the connection string from Azure KeyVault            
            //string connectionString = Config["ConnectionStrings:" + _controller + "Connection"]; //Get the connection string from appsettings
            if (string.IsNullOrWhiteSpace(connectionString)) throw new CustomException(404, $"SQL Connection String Is Missing For '{_controller}' Controller");
            using (var conn = new SqlConnection(connectionString))
            {
                DynamicParameters parameters = new();
                if (actionType == "GET")
                {
                    if (string.IsNullOrWhiteSpace(id)) errorMsg = await ValidJsonDataAsync(action, JsonDocument.Parse("{}").RootElement, actionType);
                    else if (id.All(char.IsDigit)) errorMsg = await ValidJsonDataAsync(action, JsonDocument.Parse("{\"parameter\": " + id + "}").RootElement, actionType);
                    else errorMsg = await ValidJsonDataAsync(action, JsonDocument.Parse("{\"parameter\": \"" + id + "\"}").RootElement, actionType);
                    if (errorMsg != "") throw new CustomException(400, errorMsg);
                    if (!string.IsNullOrWhiteSpace(id)) parameters.Add("Id", id);
                }
                else
                {                    
                    errorMsg = await ValidJsonDataAsync(action, payload, actionType);
                    if (errorMsg != "") throw new CustomException(400, errorMsg);
                    parameters.Add("Json", JsonSerializer.Serialize(payload));
                }
                parameters.Add("UserId", Context.HttpContext.User.FindFirst("preferred_username").Value.Split('@')[0]);
                //conn.Open();
                result = conn.ExecuteScalar<string>(procedure, parameters, null, 120, CommandType.StoredProcedure);
                conn.Close();
                if (string.IsNullOrWhiteSpace(result)) throw new CustomException(500, "Sql Server returns no data");
            };
            return result; 
        }

        private JsonNode GetJsonSchema()
        {
            JsonNode output = Cache.Get<JsonNode>("JsonSchema");
            if (output is not null) return output;
            output = JsonNode.Parse(System.IO.File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + @"\SchemaRepo.json"));
            Cache.Set("JsonSchema", output, TimeSpan.FromDays(1));
            return output;
        }

        /// <summary>
        /// Valids the json data asynchronous.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<string> ValidJsonDataAsync(string action, JsonElement payload)
        {
            return await ValidJsonDataAsync(action, payload, "post");
        }

        private async Task<string> ValidJsonDataAsync(string action, JsonElement payload, string actionType)
        {
            if (payload.ValueKind == JsonValueKind.Undefined) return "Bad Request: Invalid Json Format";
            JsonNode schema = GetJsonSchema()[(_controller + actionType).ToLower() + "_schema"]?[action.ToLower()];
            if (schema == null) return "";
            JsonSchema jschema = await JsonSchema.FromJsonAsync(schema.ToString());
            var errors = jschema.Validate(payload.GetRawText().ToLower());
            if (errors.Count > 0) return "Bad Request: " + string.Join(", ", errors.Select(e => $"{e}"));
            return "";
        }
    }
}