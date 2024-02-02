using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace METalAPI
{
    /// <summary>
    /// Operation Filter Class.
    /// </summary>
    public class SwaggerExt(IMemoryCache Cache) : IOperationFilter
    {
        private static readonly JsonSerializerSettings _jsonSettings = new() { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        /// Populates dropdown list for dynamic actions
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null || operation.Parameters.Count == 0) return;
            ControllerActionDescriptor actionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            var GetAttribute = context.MethodInfo.GetCustomAttributes(typeof(HttpGetAttribute), true).FirstOrDefault();
            Dictionary<string, object> dict = GetSpJson();
            Dictionary<string, object> dict3 = GetJsonRequests();
            KeyValuePair<string, object> item; KeyValuePair<string, object> item3; List<IOpenApiAny> myEnum = [];
            Dictionary<string, OpenApiExample> MyExamples = [];
            foreach (OpenApiParameter parameter in operation.Parameters)
            {
                if (parameter.Name.Equals("actionname", StringComparison.OrdinalIgnoreCase))
                {
                    if (GetAttribute == null)
                    {
                        item = dict.Where(x => x.Key.Equals(actionDescriptor.ControllerName + "post", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        item3 = dict3.Where(x => x.Key.Equals(actionDescriptor.ControllerName + "post_request", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (!item3.Equals(default(KeyValuePair<string, object>)))
                        {
                            Dictionary<string, object> dict4 = JsonConvert.DeserializeObject<Dictionary<string, object>>(item3.Value.ToString(), _jsonSettings);
                            foreach (KeyValuePair<string, object> item4 in dict4) MyExamples.Add(item4.Key, new OpenApiExample() { Value = new OpenApiString(item4.Value.ToString()) });
                            operation.RequestBody = new OpenApiRequestBody();
                            operation.RequestBody.Content.Add("application/json", new OpenApiMediaType() { Schema = new OpenApiSchema() { Type = "object" }, Examples = MyExamples });
                        }
                    }
                    else item = dict.Where(x => x.Key.Equals(actionDescriptor.ControllerName + "get", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (!item.Equals(default(KeyValuePair<string, object>)))
                    {
                        Dictionary<string, string> dict2 = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.Value.ToString(), _jsonSettings);
                        foreach (KeyValuePair<string, string> item2 in dict2) myEnum.Add(new OpenApiString(item2.Key));
                        parameter.Schema = new OpenApiSchema { Type = "string", Enum = myEnum };
                    }
                }
                else if (parameter.Name.Equals("parametervalue", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.AllowEmptyValue = true;
                }
            }
        }

        private Dictionary<string, object> GetJsonRequests()
        {
            Dictionary<string, object> output = Cache.Get<Dictionary<string, object>>("JsonRequests");
            if (output is not null) return output;
            output = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(Directory.GetCurrentDirectory() + @"\RequestRepo.json"), _jsonSettings);
            Cache.Set("JsonRequests", output, TimeSpan.FromDays(1));
            return output;
        }

        private Dictionary<string, object> GetSpJson()
        {
            Dictionary<string, object> output = Cache.Get<Dictionary<string, object>>("SpJson");
            if (output is not null) return output;
            output = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(Directory.GetCurrentDirectory() + @"\SPRepo.json"), _jsonSettings);
            Cache.Set("SpJson", output, TimeSpan.FromDays(1));
            return output;
        }
    }
}