using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace METalAPI
{
    /// <summary>
    /// Calls the Graph API service.
    /// </summary>
    public class GraphRepository(IConfiguration Config, IHttpContextAccessor Context) : IGraphRepository
    {
        private readonly string accesstoken = Context.HttpContext.GetTokenAsync("access_token").Result;
        private static readonly string[] requestConfiguration = ["subject", "bodyPreview", "organizer", "start", "end"];
        private static readonly string[] requestConfigurationArray = ["start/dateTime"];

        /// <summary>
        /// Returns Graph Client
        /// </summary>
        public GraphServiceClient GetGraphClient()
        {
            var scopes = new[] { ".default" };
            var onBehalfOfCredential = new OnBehalfOfCredential(Config["AzureAd:tenantId"], Config["AzureAd:ClientId"], Config["AzureAd:ClientSecret"], accesstoken);
            var tokenRequestContext = new TokenRequestContext(scopes);
            #pragma warning disable IDE0059 // Unnecessary assignment of a value
            ValueTask<AccessToken> token = onBehalfOfCredential.GetTokenAsync(tokenRequestContext, new CancellationToken());
            #pragma warning restore IDE0059 // Unnecessary assignment of a value
            return new GraphServiceClient(onBehalfOfCredential, scopes);
        }

        /// <summary>
        /// Returns Calendar View.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<string> CalendarView(JsonElement payload)
        {
            GraphServiceClient graphClient = GetGraphClient();
            string TimeZone = await GetTimeZone(graphClient);
            var jsonNode = JsonNode.Parse(payload.GetRawText(), new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            DateTimeOffset StartDate; DateTimeOffset EndDate;
            string startdatetime = jsonNode["startdate"]?.ToString();
            string enddatetime = jsonNode["enddate"]?.ToString();
            if (string.IsNullOrWhiteSpace(startdatetime) && string.IsNullOrWhiteSpace(enddatetime))
            {
                StartDate = ChangeUtcToUserLocal(DateTime.UtcNow.Date, TimeZone);
                EndDate = StartDate.AddDays(1).AddMilliseconds(-1);
            }
            else if (string.IsNullOrWhiteSpace(startdatetime))
            {
                if (string.IsNullOrWhiteSpace(enddatetime)) EndDate = ChangeUtcToUserLocal(DateTime.UtcNow.Date, TimeZone).AddDays(1).AddMilliseconds(-1);
                else EndDate = ChangeUtcToUserLocal(DateTime.SpecifyKind(Convert.ToDateTime(enddatetime), DateTimeKind.Utc), TimeZone).AddDays(1).AddMilliseconds(-1);
                StartDate = EndDate.AddDays(-1).AddMilliseconds(1);
            }
            else if (string.IsNullOrWhiteSpace(enddatetime))
            {
                if (string.IsNullOrWhiteSpace(startdatetime)) StartDate = ChangeUtcToUserLocal(DateTime.UtcNow.Date, TimeZone);
                else StartDate = ChangeUtcToUserLocal(DateTime.SpecifyKind(Convert.ToDateTime(startdatetime), DateTimeKind.Utc), TimeZone);
                EndDate = StartDate.AddDays(1).AddMilliseconds(-1);
            }
            else
            {
                StartDate = ChangeUtcToUserLocal(DateTime.SpecifyKind(Convert.ToDateTime(startdatetime), DateTimeKind.Utc), TimeZone);
                EndDate = ChangeUtcToUserLocal(DateTime.SpecifyKind(Convert.ToDateTime(enddatetime), DateTimeKind.Utc), TimeZone).AddDays(1).AddMilliseconds(-1);
            }
            var result = await graphClient.Me.CalendarView.GetAsync(x =>
            {
                x.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZone + "\"");
                x.QueryParameters.Select = requestConfiguration;
                x.QueryParameters.StartDateTime = StartDate.ToString();
                x.QueryParameters.EndDateTime = EndDate.ToString();
                x.QueryParameters.Orderby = requestConfigurationArray;
            });
            string jsonString = JsonConvert.SerializeObject(result);
            JToken token = JToken.Parse(jsonString);
            var count = token.SelectTokens("$.Value[*]").Count();
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "", data = new { totalRecords = count, resultSet = token.SelectToken("Value") } });
        }

        /// <summary>
        /// Returns ToDo Tasks
        /// </summary>
        public async Task<string> ToDoTasks()
        {
            GraphServiceClient graphClient = GetGraphClient();
            string todoTaskListId = await GetTodoTaskListId(graphClient);
            if (todoTaskListId == "") throw new CustomException(404, "ToDo Tasks Folder Not Found");
            var results = await graphClient.Me.Todo.Lists[todoTaskListId].Tasks.GetAsync(x => x.QueryParameters.Filter = "status ne 'completed'");
            string jsonString = JsonConvert.SerializeObject(results);
            JToken token = JToken.Parse(jsonString);
            var count = token.SelectTokens("$.Value[*]").Count();
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "", data = new { totalRecords = count, resultSet = token.SelectToken("Value") } });
        }

        /// <summary>
        /// Sends the mail.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<string> SendMail(JsonElement payload)
        {
            if (payload.ValueKind == JsonValueKind.Undefined) throw new CustomException(400, "Bad Request: Invalid Json Format");
            var jsonNode = JsonNode.Parse(payload.GetRawText(), new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            string subject = jsonNode["subject"]?.ToString();
            string recipients = jsonNode["toRecipients"]?.ToString();
            string ccRecipients = jsonNode["ccRecipients"]?.ToString();
            string body = jsonNode["body"]?.ToString();
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(recipients)) throw new CustomException(400, "Bad Request");
            else
            {
                body ??= "";
                var message = new Message { Subject = subject, Body = new ItemBody { ContentType = BodyType.Text, Content = body, }, ToRecipients = GetRecipients(recipients), };
                if (!string.IsNullOrWhiteSpace(ccRecipients)) message.CcRecipients = GetRecipients(ccRecipients);
                var requestBody = new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody { Message = message, SaveToSentItems = true, };
                var graphClient = GetGraphClient();
                await graphClient.Me.SendMail.PostAsync(requestBody);
                return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "" });
            }
        }

        /// <summary>
        /// Returns User Profile Photo
        /// </summary>
        public async Task<string> ProfilePhoto()
        {
            var graphClient = GetGraphClient();
            System.IO.Stream photo = await graphClient.Me.Photos["64x64"].Content.GetAsync();
            using var ms = new System.IO.MemoryStream();
            photo.CopyTo(ms);
            var photoBytes = ms.ToArray();
            var result = Convert.ToBase64String(photoBytes);
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "", data = new { totalRecords = 1, resultSet = new[] { new { photo = result } } } });
        }

        /// <summary>
        /// Returns Manager Info
        /// </summary>
        public async Task<string> ManagerInfo()
        {
            var graphClient = GetGraphClient();
            var result = await graphClient.Me.Manager.GetAsync();
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "", data = new { totalRecords = 1, resultSet = new[] { JToken.FromObject(result) } } });
        }

        /// <summary>
        /// Returns Directory Roles
        /// </summary>
        public async Task<string> ADRoles()
        {
            var graphClient = GetGraphClient();
            var results = await graphClient.DirectoryRoles.GetAsync();
            string jsonString = JsonConvert.SerializeObject(results);
            JToken token = JToken.Parse(jsonString);
            var count = token.SelectTokens("$.Value[*]").Count();
            return JsonConvert.SerializeObject(new { statusCode = 200, errorMessage = "", data = new { totalRecords = count, resultSet = token.SelectToken("Value") } });
        }

        /// <summary>
        /// Returns Recipients.
        /// </summary>
        /// <param name="recipients">The recipients.</param>
        /// <returns></returns>
        protected List<Recipient> GetRecipients(string recipients)
        {
            List<Recipient> recipientsList = [];
            foreach (var addr in recipients.Split([',', ';']))
            {
                Recipient recipient = new() { EmailAddress = new EmailAddress() };
                recipient.EmailAddress.Address = addr;
                recipientsList.Add(recipient);
            }
            return recipientsList;
        }

        /// <summary>
        /// Returns the time zone.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <returns></returns>
        public async Task<string> GetTimeZone(GraphServiceClient graphClient)
        {
            var result = await graphClient.Me.MailboxSettings.GetAsync();
            string jsonString = JsonConvert.SerializeObject(result);
            return JToken.Parse(jsonString).SelectToken("TimeZone").ToString();
        }

        /// <summary>
        /// Changes the UTC to user local timezone.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="TimeZone">The time zone.</param>
        /// <returns></returns>
        protected DateTimeOffset ChangeUtcToUserLocal(DateTimeOffset original, string TimeZone)
        {
            TimeZoneInfo zInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            DateTimeOffset oTime = TimeZoneInfo.ConvertTime(original, zInfo);
            return original.Subtract(oTime.Offset).ToOffset(oTime.Offset);
        }

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
        public async Task<string> AddToDoTask(GraphServiceClient graphClient, string todoTaskListId, string title, string startDate, string dueDate = null, string body = null)
        {
            string TimeZone = await GetTimeZone(graphClient);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            var requestBody = new TodoTask { Title = title, Body = new ItemBody { Content = body ?? "" }, StartDateTime = Convert.ToDateTime(startDate).ToDateTimeTimeZone(tz) };
            if (!string.IsNullOrWhiteSpace(dueDate)) requestBody.DueDateTime = Convert.ToDateTime(dueDate).ToDateTimeTimeZone(tz);
            var result = await graphClient.Me.Todo.Lists[todoTaskListId].Tasks.PostAsync(requestBody);
            return JToken.Parse(JsonConvert.SerializeObject(result)).SelectToken("Id").ToString();
        }

        /// <summary>
        /// Returns todoTaskListId.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <returns></returns>
        public async Task<string> GetTodoTaskListId(GraphServiceClient graphClient)
        {
            var resultt = await graphClient.Me.Todo.Lists.GetAsync(x => { x.QueryParameters.Filter = "displayName eq 'Tasks'"; x.QueryParameters.Top = 1; });
            string jsonString = JsonConvert.SerializeObject(resultt);
            var items = JToken.Parse(jsonString).SelectTokens("$.Value[0].Id");
            string TaskId = "";
            foreach (JToken itm in items)
            {
                TaskId = itm.ToString();
                break;
            }
            return TaskId;
        }

        /// <summary>
        /// Deletes the todo task.
        /// </summary>
        /// <param name="graphClient">The graph client.</param>
        /// <param name="todoTaskListId">The todo task list identifier.</param>
        /// <param name="todoTaskId">The todo task identifier.</param>
        public async Task DeleteTodoTask(GraphServiceClient graphClient, string todoTaskListId, string todoTaskId)
        {
            await graphClient.Me.Todo.Lists[todoTaskListId].Tasks[todoTaskId].DeleteAsync();
        }
    }
}