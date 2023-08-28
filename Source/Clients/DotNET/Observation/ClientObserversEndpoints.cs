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
    /// <returns><see cref="IEndpointRouteBuilder"/> for build continuation.</returns>
    public static IEndpointRouteBuilder MapClientObservers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/.cratis/observers/{observerId}", async (HttpContext context) =>
        {
            if (context.GetRouteValue("observerId") is not string observerIdAsString)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            ObserverId observerId;
            try
            {
                observerId = (ObserverId)observerIdAsString;
            }
            catch
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var events = await context.Request.ReadFromJsonAsync<IEnumerable<AppendedEvent>>(Globals.JsonSerializerOptions);
            if (events is null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            CommandResult result;
            var observers = context.RequestServices.GetRequiredService<ClientObservers>();
            var lastSuccessfullyObservedEvent = await observers.OnNext(observerId, events);
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                result = new CommandResult
                {
                    Response = lastSuccessfullyObservedEvent
                };
            }
            catch (Exception ex)
            {
                result = new CommandResult
                {
                    ExceptionMessages = ex.GetAllMessages(),
                    ExceptionStackTrace = ex.StackTrace ?? string.Empty,
                    Response = lastSuccessfullyObservedEvent
                };
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            await context.Response.WriteAsJsonAsync(result, Globals.JsonSerializerOptions);
        });

        return endpoints;
    }
}
