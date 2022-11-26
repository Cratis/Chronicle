// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (context.HttpContext.Request.Method == HttpMethod.Get.Method
            && context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var result = await next();
            if (result.Result is ObjectResult objectResult)
            {
                switch (objectResult.Value)
                {
                    case IClientObservable clientObservable:
                        {
                            _logger.ClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
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
                        break;

                    default:
                        {
                            _logger.NonClientObservableReturnValue(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName);
                            result.Result = new ObjectResult(new QueryResult(objectResult.Value!, true));
                        }
                        break;
                }
            }
        }
        else
        {
            await next();
        }
    }
}
