using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace METalAPI
{
    /// <summary>
    /// Coordinates the work of multiple repositories.
    /// </summary>
    public class UnitOfWork(IDbRepository DbRepo, IGraphRepository GraphRepo, IRabbitMqRepository MQRepo) : IUnitOfWork
    {
        /// <summary>
        /// Gets Db Repository
        /// </summary>
        public IDbRepository Db
        {
            get { return DbRepo; }
        }
        /// <summary>
        /// Gets Graph Repository
        /// </summary>
        public IGraphRepository Graph
        {
            get { return GraphRepo; }
        }

        /// <summary>
        /// Gets Graph Repository
        /// </summary>
        public IRabbitMqRepository MQ
        {
            get { return MQRepo; }
        }

        /// <summary>
        /// Adds ToDo Task.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns>Todo Id</returns>
        public async Task<string> AddTask(JsonElement payload)
        {
            string errorMsg = await DbRepo.ValidJsonDataAsync("AddTask", payload);
            if (errorMsg != "") throw new CustomException(400, errorMsg);
            var jsonNode = JsonNode.Parse(payload.GetRawText(), new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            string startDate = jsonNode["startdatetime"]?.ToString();
            string dueDate = jsonNode["enddatetime"]?.ToString();
            string title = jsonNode["title"]?.ToString();
            string body = jsonNode["description"]?.ToString();
            var graphClient = GraphRepo.GetGraphClient();
            string todoTaskListId = await GraphRepo.GetTodoTaskListId(graphClient);
            string todoTaskId = await GraphRepo.AddToDoTask(graphClient, todoTaskListId, title, startDate, dueDate, body);
            try
            {
                jsonNode["outlookmeetingid"] = todoTaskId;
                payload = JsonDocument.Parse(jsonNode.ToJsonString()).RootElement;
                string result = await DbRepo.Query("AddTask", payload: payload);
                if (result.IndexOf("todo_id", StringComparison.OrdinalIgnoreCase) > -1) return result;
            }
            catch (Exception ex)
            {
                await GraphRepo.DeleteTodoTask(graphClient, todoTaskListId, todoTaskId);
                throw new CustomException(500, ex.Message);
            }
            await GraphRepo.DeleteTodoTask(graphClient, todoTaskListId, todoTaskId);
            throw new CustomException(400, "Bad Request: Failed To Add Task In DB");            
        }

        /// <summary>
        /// Returns Calendar View.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<string> CalendarView(JsonElement payload)
        {
            string errorMsg = await DbRepo.ValidJsonDataAsync("CalendarView", payload);
            if (errorMsg != "") throw new CustomException(400, errorMsg);
            return await GraphRepo.CalendarView(payload);
        }
    }
}