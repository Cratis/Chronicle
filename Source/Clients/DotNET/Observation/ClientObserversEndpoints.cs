// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Aksio.Commands;
using Aksio.Cratis.Events;
using Aksio.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents the endpoints for the client observers.
/// </summary>
public static class ClientObserversEndpoints
{
    /// <summary>
    /// Maps the endpoints for the client observers.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to extend.</param>
    public static void MapClientObservers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/.cratis/observers/{observerId}", async (HttpContext context) =>
        {
            if (context.GetRouteValue("observerId") is not string observerId)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var observer = (ObserverId)observerId;
            var @event = await context.Request.ReadFromJsonAsync<AppendedEvent>(Globals.JsonSerializerOptions);
            if (@event is null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            CommandResult result;
            var observers = context.RequestServices.GetRequiredService<ClientObservers>();
            try
            {
                await observers.OnNext(observerId, @event);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                result = CommandResult.Success;
            }
            catch (Exception ex)
            {
                result = new CommandResult
                {
                    ExceptionMessages = ex.GetAllMessages(),
                    ExceptionStackTrace = ex.StackTrace ?? string.Empty
                };
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            await context.Response.WriteAsJsonAsync(result, Globals.JsonSerializerOptions);
        });
    }
}
