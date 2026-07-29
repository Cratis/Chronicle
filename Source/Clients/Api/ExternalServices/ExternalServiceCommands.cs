// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ModelBinding;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Security;
using IExternalServicesService = Cratis.Chronicle.Contracts.ExternalServices.IExternalServices;

namespace Cratis.Chronicle.Api.ExternalServices;

/// <summary>
/// Represents the API for working with external service commands.
/// </summary>
[Route("/api/event-store/{eventStore}/external-services")]
public class ExternalServiceCommands : ControllerBase
{
    readonly IExternalServicesService _externalServices;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceCommands"/> class.
    /// </summary>
    /// <param name="externalServices"><see cref="IExternalServicesService"/> for working with external services.</param>
    internal ExternalServiceCommands(IExternalServicesService externalServices)
    {
        _externalServices = externalServices;
    }

    /// <summary>
    /// Add a new external service.
    /// </summary>
    /// <param name="command">Command for adding the external service.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public async Task AddExternalService(
        [FromRequest] AddExternalService command)
    {
        var endpoint = new Contracts.ExternalServices.ExternalServiceEndpoint
        {
            Type = (Contracts.ExternalServices.ExternalServiceEndpointType)command.EndpointType
        };

        if (command.EndpointType == ExternalServiceEndpointType.Http)
        {
            endpoint.Http = new Contracts.ExternalServices.HttpEndpointConfiguration
            {
                Url = command.Url ?? string.Empty,
                Authorization = CreateAuthorization(command),
                Headers = command.Headers.ToDictionary(h => h.Key, h => h.Value)
            };
        }
        else
        {
            endpoint.Database = new Contracts.ExternalServices.DatabaseEndpointConfiguration
            {
                Host = command.Host ?? string.Empty,
                Port = command.Port,
                Database = command.Database ?? string.Empty,
                Username = command.Username ?? string.Empty,
                Password = command.Password ?? string.Empty,
                Options = command.Options.ToDictionary(o => o.Key, o => o.Value)
            };
        }

        await _externalServices.Add(new Contracts.ExternalServices.AddExternalServices
        {
            EventStore = command.EventStore,
            ExternalServices =
            [
                new Contracts.ExternalServices.ExternalServiceDefinition
                {
                    Id = string.IsNullOrEmpty(command.Id) ? command.Name : command.Id,
                    Name = command.Name,
                    Endpoint = endpoint
                }
            ]
        });
    }

    /// <summary>
    /// Remove an external service.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for removing the external service.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("remove")]
    public async Task RemoveExternalService(
        [FromRoute] string eventStore,
        [FromBody] RemoveExternalService command)
    {
        await _externalServices.Remove(new Contracts.ExternalServices.RemoveExternalServices
        {
            EventStore = eventStore,
            ExternalServices = [command.ExternalServiceId]
        });
    }

    static OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? CreateAuthorization(AddExternalService command) =>
        command.AuthorizationType switch
        {
            Security.AuthorizationType.Basic => new(new BasicAuthorization
            {
                Username = command.BasicUsername ?? string.Empty,
                Password = command.BasicPassword ?? string.Empty
            }),
            Security.AuthorizationType.Bearer => new(new BearerTokenAuthorization
            {
                Token = command.BearerToken ?? string.Empty
            }),
            Security.AuthorizationType.OAuth => new(new OAuthAuthorization
            {
                Authority = command.OAuthAuthority ?? string.Empty,
                ClientId = command.OAuthClientId ?? string.Empty,
                ClientSecret = command.OAuthClientSecret ?? string.Empty
            }),
            _ => null
        };
}
