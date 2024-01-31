using System.Text.Json;
using System.Threading.Tasks;

namespace CollaberaAPI
{
    /// <summary>
    /// Coordinates the work of multiple repositories.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Gets Db Repository
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        IDbRepository Db { get; }

        /// <summary>
        /// Gets Graph Repository.
        /// </summary>
        /// <value>
        /// The graph.
        /// </value>
        IGraphRepository Graph { get; }

        /// <summary>
        /// Gets RabbitMq Repository
        /// </summary>
        /// <value>
        /// The mq.
        /// </value>
        IRabbitMqRepository MQ { get; }

        /// <summary>
        /// Adds the task.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        Task<string> AddTask(JsonElement payload);

        /// <summary>
        /// Returns Calendar View.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        Task<string> CalendarView(JsonElement payload);
    }
}