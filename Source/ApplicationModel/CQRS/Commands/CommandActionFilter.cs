// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aksio.Cratis.Applications.Commands;

/// <summary>
/// Represents a <see cref="IAsyncActionFilter"/> for providing a proper <see cref="CommandResult"/> for post actions.
/// </summary>
public class CommandActionFilter : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (context.HttpContext.Request.Method == HttpMethod.Post.Method)
        {
            result.Result = new ObjectResult(new CommandResult(true));
        }
    }
}
