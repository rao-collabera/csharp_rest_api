using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CollaberaAPI
{
    /// <summary>
    /// Coordinates the work of multiple repositories.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbRepository _dbRepository;
        private readonly IGraphRepository _graph;
        private readonly IRabbitMqRepository _mq;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="DbRepo">Db Repository</param>
        /// <param name="GraphRepo">Graph Repository</param>
        /// <param name="MQRepo">RabbitMQ Repository</param>
        public UnitOfWork(IDbRepository DbRepo, IGraphRepository GraphRepo, IRabbitMqRepository MQRepo)
        {
            _dbRepository = DbRepo;
            _graph = GraphRepo;
            _mq = MQRepo;
        }

        /// <summary>
        /// Gets Db Repository
        /// </summary>
        public IDbRepository Db
        {
            get { return _dbRepository; }
        }
        /// <summary>
        /// Gets Graph Repository
        /// </summary>
        public IGraphRepository Graph
        {
            get { return _graph; }
        }

        /// <summary>
        /// Gets Graph Repository
        /// </summary>
        public IRabbitMqRepository MQ
        {
            get { return _mq; }
        }

        /// <summary>
        /// Adds ToDo Task.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns>Todo Id</returns>
        public async Task<string> AddTask(JsonElement payload)
        {
            string errorMsg = await _dbRepository.ValidJsonDataAsync("AddTask", payload);
            if (errorMsg != "") throw new CustomException(400, errorMsg);
            var jsonNode = JsonNode.Parse(payload.GetRawText(), new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            string startDate = jsonNode["startdatetime"]?.ToString();
            string dueDate = jsonNode["enddatetime"]?.ToString();
            string title = jsonNode["title"]?.ToString();
            string body = jsonNode["description"]?.ToString();
            var graphClient = _graph.GetGraphClient();
            string todoTaskListId = await _graph.GetTodoTaskListId(graphClient);
            string todoTaskId = await _graph.AddToDoTask(graphClient, todoTaskListId, title, startDate, dueDate, body);
            try
            {
                jsonNode["outlookmeetingid"] = todoTaskId;
                payload = JsonDocument.Parse(jsonNode.ToJsonString()).RootElement;
                string result = await _dbRepository.Query("AddTask", payload: payload);
                if (result.IndexOf("todo_id", StringComparison.OrdinalIgnoreCase) > -1) return result;
            }
            catch (Exception ex)
            {
                await _graph.DeleteTodoTask(graphClient, todoTaskListId, todoTaskId);
                throw new CustomException(500, ex.Message);
            }
            await _graph.DeleteTodoTask(graphClient, todoTaskListId, todoTaskId);
            throw new CustomException(400, "Bad Request: Failed To Add Task In DB");            
        }

        /// <summary>
        /// Returns Calendar View.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<string> CalendarView(JsonElement payload)
        {
            string errorMsg = await _dbRepository.ValidJsonDataAsync("CalendarView", payload);
            if (errorMsg != "") throw new CustomException(400, errorMsg);
            return await _graph.CalendarView(payload);
        }
    }
}