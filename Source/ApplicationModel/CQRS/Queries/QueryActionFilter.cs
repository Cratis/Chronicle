// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Validation;
using Aksio.Cratis.Queries;
using Aksio.Cratis.Strings;
using Aksio.Cratis.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Applications.Queries;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="QueryResult"/> for post actions.
/// </summary>
public class QueryActionFilter : IAsyncActionFilter
{
    readonly JsonOptions _options;
    readonly ILogger<QueryActionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryActionFilter"/> class.
    /// </summary>
    /// <param name="options"><see cref="JsonOptions"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public QueryActionFilter(
        IOptions<JsonOptions> options,
        ILogger<QueryActionFilter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method == HttpMethod.Get.Method &&
            context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var exceptionMessages = new List<string>();
            var exceptionStackTrace = string.Empty;
            object? response = null;
            ActionExecutedContext? result = null;

            if (context.ModelState.IsValid)
            {
                result = await next();

                if (result.Exception is not null)
                {
                    var exception = result.Exception;
                    exceptionStackTrace = exception.StackTrace;

                    do
                    {
                        exceptionMessages.Add(exception.Message);
                        exception = exception.InnerException;
                    }
                    while (exception is not null);

                    result.Exception = null!;
                }

                if (result.Result is ObjectResult objectResult)
                {
                    response = objectResult.Value;
                }
            }

            if (result?.Result is ObjectResult or && or.Value is IClientObservable clientObservable)
            {
                _logger.ClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
                HandleWebSocketHeadersForMultipleProxies(context.HttpContext);
                if (context.HttpContext.WebSockets.IsWebSocketRequest)
                {
                    _logger.RequestIsWebSocket();
                    await clientObservable.HandleConnection(context, _options);
                    result.Result = null;
                }
                else
                {
                    _logger.RequestIsHttp();
                }
            }
            else
            {
                _logger.NonClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
                var queryResult = new QueryResult
                {
                    ValidationErrors = context.ModelState.SelectMany(_ => _.Value!.Errors.Select(p => p.ToValidationResult(_.Key.ToCamelCase()))),
                    ExceptionMessages = exceptionMessages.ToArray(),
                    ExceptionStackTrace = exceptionStackTrace ?? string.Empty,
                    Data = response!
                };

                if (!queryResult.IsAuthorized)
                {
                    context.HttpContext.Response.StatusCode = 401;   // Forbidden: https://www.rfc-editor.org/rfc/rfc9110.html#name-401-unauthorized
                }
                else if (!queryResult.IsValid)
                {
                    context.HttpContext.Response.StatusCode = 409;   // Conflict: https://www.rfc-editor.org/rfc/rfc9110.html#name-409-conflict
                }
                else if (queryResult.HasExceptions)
                {
                    context.HttpContext.Response.StatusCode = 500;  // Internal Server error: https://www.rfc-editor.org/rfc/rfc9110.html#name-500-internal-server-error
                }

                var actualResult = new ObjectResult(queryResult);

                if (result is not null)
                {
                    result.Result = actualResult;
                }
                else
                {
                    context.Result = actualResult;
                }
            }
        }
        else
        {
            await next();
        }
    }

    /// <summary>
    /// Handles the Web Socket headers for connections that are going through multiple proxies.
    /// </summary>
    /// <param name="httpContext"><see cref="HttpContext"/> to handle for.</param>
    /// <remarks>
    /// In the ASP.NET Core 6 code there is a middleware called WebSocketMiddleware. The WebSocketManager
    /// that we ask for .IsWebSocketRequest forwards this call to it.
    /// This property calls internally a method called CheckSupportedWebSocketRequest which will check
    /// the following Http Headers for valid values:
    /// Upgrade with value Upgrade
    /// Connection with value websocket
    /// <p/>
    /// If they are correct, it will consider it an upgrade of the protocol and will then validate and use
    /// the values from the Web Socket specific headers:
    /// Sec-WebSocket-Protocol
    /// Sec-WebSocket-Extensions
    /// Sec-WebSocket-Version
    /// Sec-WebSocket-Key
    /// <p/>
    /// When running in an environment with multiple reverse proxies you can end up with the proxy adding
    /// to the values if the values are already there, forming a collection of values as comma separated
    /// values in the HTTP header.
    /// The validation code in ASP.NET validates that the version is supported and that the key is valid.
    /// Throughout the validation code in ASP.NET it recognizes the fact that it could hold multiple values
    /// and loops through the values, except for the key - which it just does .ToString() on, which then
    /// gives you the comma separated string.
    /// The key is expected to be a base64 encoded byte array of 16 bytes, and obviously this would not
    /// then be valid and we're not allowed to upgrade the connection.
    /// The purpose of the key coming from the client is to use it and combine with a server key and send
    /// back on the response to form a valid connection.
    /// This code basically recognizes this problem and assumes that the last key is the one from the client
    /// and strips away any other keys and uses it instead.
    /// </remarks>
    void HandleWebSocketHeadersForMultipleProxies(HttpContext httpContext)
    {
        var keys = httpContext.Request.Headers.SecWebSocketKey.ToString().Split(',').Select(_ => _.Trim()).ToArray();
        if (keys.Length > 1)
        {
            httpContext.Request.Headers.SecWebSocketKey = keys[^1];
        }
    }
}
