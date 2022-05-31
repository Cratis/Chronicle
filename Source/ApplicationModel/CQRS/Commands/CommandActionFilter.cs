// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aksio.Cratis.Applications.Commands;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="CommandResult"/> for post actions.
/// </summary>
public class CommandActionFilter : IAsyncActionFilter
{
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandActionFilter"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting execution context.</param>
    public CommandActionFilter(IExecutionContextManager executionContextManager)
    {
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var exceptionMessages = new List<string>();
        var exceptionStackTrace = string.Empty;
        if (context.ModelState.IsValid)
        {
            var result = await next();

            if (result.Exception is not null)
            {
                var exception = result.Exception;

                do
                {
                    exceptionMessages.Add(exception.Message);
                    exception = exception.InnerException;
                }
                while (exception is not null);
            }
        }
        if (context.HttpContext.Request.Method == HttpMethod.Post.Method)
        {
            var commandResult = new CommandResult
            {
                CorrelationId = _executionContextManager.Current.CorrelationId,
                ValidationErrors = context.ModelState.SelectMany(_ => _.Value!.Errors.Select(p => new ValidationError(p.ErrorMessage, new string[] { _.Key }))),
                ExceptionMessages = exceptionMessages.ToArray(),
                ExceptionStackTrace = exceptionStackTrace
            };

            if (!commandResult.IsAuthorized)
            {
                context.HttpContext.Response.StatusCode = 401;   // Forbidden: https://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.4.10
            }
            else if (!commandResult.IsValid)
            {
                context.HttpContext.Response.StatusCode = 409;   // Conflict: https://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.4.10
            }

            context.Result = new ObjectResult(commandResult);
        }
    }
}
