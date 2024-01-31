using Microsoft.Graph;
using System.Text.Json;
using System.Threading.Tasks;

namespace CollaberaAPI
{
    /// <summary>
    /// Calls the Graph API service.
    /// </summary>
    public interface IGraphRepository
    {
        /// <summary>
        /// Returns Graph Client
        /// </summary>
        public GraphServiceClient GetGraphClient();

        /// <summary>
        /// Returns Directory Roles
        /// </summary>
        public Task<string> ADRoles();

        /// <summary>
        /// Returns ToDo Tasks
        /// </summary>
        public Task<string> ToDoTasks();

        /// <summary>
        /// Sends the mail.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public Task<string> SendMail(JsonElement payload);

        /// <summary>
        /// Returns User Profile Photo
        /// </summary>
        public Task<string> ProfilePhoto();

        /// <summary>
        /// Returns Manager Info
        /// </summary>
        public Task<string> ManagerInfo();

        /// <summary>
        /// Returns the time zone.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <returns></returns>
        public Task<string> GetTimeZone(GraphServiceClient graphClient);

        /// <summary>
        /// Returns todoTaskListId.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <returns></returns>
        public Task<string> GetTodoTaskListId(GraphServiceClient graphClient);

        /// <summary>
        /// Returns Calendar View.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public Task<string> CalendarView(JsonElement payload);

        /// <summary>
        /// Adds Todo Task.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <param name="todoTaskListId">The todo task list identifier.</param>
        /// <param name="title">The title.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="dueDate">The due date.</param>
        /// <param name="body">The body.</param>
        /// <returns>Task Id</returns>
        public Task<string> AddToDoTask(GraphServiceClient graphClient, string todoTaskListId, string title, string startDate, string dueDate = null, string body = null);

        /// <summary>
        /// Deletes the todo task.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <param name="todoTaskListId">The todo task list identifier.</param>
        /// <param name="todoTaskId">The todo task identifier.</param>
        /// <returns></returns>
        public Task DeleteTodoTask(GraphServiceClient graphClient, string todoTaskListId, string todoTaskId);
    }
}