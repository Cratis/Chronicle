// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Commands;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="CommandResult"/> for post actions.
/// </summary>
public class CommandActionFilter : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method == HttpMethod.Post.Method)
        {
            var exceptionMessages = new List<string>();
            var exceptionStackTrace = string.Empty;
            ActionExecutedContext? result = null;
            object? response = null;
            if (context.ModelState.IsValid)
            {
                result = await next();

                if (context.IsAspNetResult()) return;

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

            var commandResult = new CommandResult<object>
            {
                // TODO: Set correlation ID to the actual correlation ID
                // CorrelationId = _executionContextManager.Current.CorrelationId,
                ValidationResults = context.ModelState.SelectMany(_ => _.Value!.Errors.Select(e => e.ToValidationResult(_.Key))),
                ExceptionMessages = exceptionMessages.ToArray(),
                ExceptionStackTrace = exceptionStackTrace ?? string.Empty,
                Response = response
            };

            if (!commandResult.IsAuthorized)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;            // Forbidden: https://www.rfc-editor.org/rfc/rfc9110.html#name-401-unauthorized
            }
            else if (!commandResult.IsValid)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;             // Conflict: https://www.rfc-editor.org/rfc/rfc9110.html#name-409-conflict
            }
            else if (commandResult.HasExceptions)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;  // Internal Server error: https://www.rfc-editor.org/rfc/rfc9110.html#name-500-internal-server-error
            }

            var actualResult = new ObjectResult(commandResult);

            if (result is not null)
            {
                result.Result = actualResult;
            }
            else
            {
                context.Result = actualResult;
            }
        }
        else
        {
            await next();
        }
    }
}
