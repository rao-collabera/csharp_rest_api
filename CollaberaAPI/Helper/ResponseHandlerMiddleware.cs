using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CollaberaAPI
{
    /// <summary>
    /// Response Handler Middleware Class.
    /// </summary>
    public class ResponseHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="next">The next.</param>
        public ResponseHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        public async Task Invoke(HttpContext httpContext)
        {
            var originalBodyStream = httpContext.Response.Body;
            using var responseBody = new MemoryStream();
            httpContext.Response.Body = responseBody;
            try
            {
                await _next(httpContext);
                httpContext.Response.ContentType = "application/json";
                if (httpContext.Response.StatusCode != 200)
                {
                    string body = ((HttpStatusCode)httpContext.Response.StatusCode).ToString();
                    await HandlenMessageAsync(httpContext, body).ConfigureAwait(false);
                }
            }
            catch (CustomException ex)
            {
                httpContext.Response.StatusCode = ex.StatusCode;
                await HandlenMessageAsync(httpContext, ex.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                httpContext.Response.StatusCode = 500;
                await HandlenMessageAsync(httpContext, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private static async Task HandlenMessageAsync(HttpContext context, string exception)
        {
            string result = JsonConvert.SerializeObject(new { statusCode = context.Response.StatusCode, errorMessage = exception });
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(result);
        }

        private static async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string plainBodyText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return plainBodyText;
        }
    }

    /// <summary>
    ///Exception Handler Middleware Extensions
    /// </summary>
    public static class ExceptionHandlerMiddlewareExtensions
    {
        /// <summary>
        ///Exception Handler Middleware Method
        /// </summary>
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseHandlerMiddleware>();
        }
    }
}