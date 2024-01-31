using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace METalAPI
{
    /// <summary>
    /// Handles user interaction.
    /// </summary>
    [Route("api/[controller]")]
    [TypeFilter(typeof(UserAuthorizationFilter))]
    public class METalController(IUnitOfWork unitOfWork) : Controller
    {
        /// <summary>
        /// Publish Messages to Queue.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        [HttpPost("SendMessage")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult SendMessage([FromBody] JsonElement payload)
        {
            return Ok(unitOfWork.MQ.SendMessage(payload));
        }

        ///Added two same GET actions (.NET Core allows optional route parameters, but path parameters are always required in OpenAPI 3.0)
        /// <summary>
        /// Returns Requested Record.
        /// </summary>
        /// <param name="ActionName">Name of the action.</param>
        /// <returns></returns>
        [HttpGet("{ActionName}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetResults(string ActionName)
        {
            return Ok(await unitOfWork.Db.Query(ActionName, null));
        }

        /// <summary>
        /// Returns Requested Record.
        /// </summary>
        /// <param name="ActionName">Name of the action.</param>
        /// <param name="ParameterValue">The parameter value.</param>
        /// <returns></returns>
        [HttpGet("{ActionName}/{ParameterValue}")]
        public async Task<IActionResult> GetResults(string ActionName, string ParameterValue)
        {
            return Ok(await unitOfWork.Db.Query(ActionName, ParameterValue));
        }

        /// <summary>
        /// Adds ToDo Task.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns>Todo Id</returns>
        [HttpPost("AddTask")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> AddTask([FromBody] JsonElement payload)
        {
            return Ok(await unitOfWork.AddTask(payload));
        }

        /// <summary>
        /// Returns Calendar View.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        [HttpPost("CalendarView")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> CalendarView([FromBody] JsonElement payload)
        {
            return Ok(await unitOfWork.CalendarView(payload));
        }

        /// <summary>
        /// Returns Resultset
        /// </summary>
        /// <param name="ActionName"></param>
        /// <param name="payload"></param>
        [HttpPost("{ActionName}")]
        public async Task<IActionResult> PostResults(string ActionName, [FromBody] JsonElement payload)
        {
            return Ok(await unitOfWork.Db.Query(ActionName, payload: payload));
        }
    }
}