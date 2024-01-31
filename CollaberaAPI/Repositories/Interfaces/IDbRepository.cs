using System.Text.Json;
using System.Threading.Tasks;

namespace CollaberaAPI
{
    /// <summary>
    /// Handles DB Calls.
    /// </summary>
    public interface IDbRepository
    {
        /// <summary>
        /// Returns Requested Record.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="actionID">The action identifier.</param>
        /// <returns></returns>
        Task<string> Query(string actionName, string actionID);

        /// <summary>
        /// Returns Resultset.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        Task<string> Query(string actionName, JsonElement payload);

        /// <summary>
        /// Valids the json data asynchronous.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        Task<string> ValidJsonDataAsync(string action, JsonElement payload);
    }
}