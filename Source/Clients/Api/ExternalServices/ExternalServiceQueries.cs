// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IExternalServicesService = Cratis.Chronicle.Contracts.ExternalServices.IExternalServices;

namespace Cratis.Chronicle.Api.ExternalServices;

/// <summary>
/// Represents the API for working with external service queries.
/// </summary>
[Route("/api/event-store/{eventStore}/external-services")]
public class ExternalServiceQueries : ControllerBase
{
    readonly IExternalServicesService _externalServices;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceQueries"/> class.
    /// </summary>
    /// <param name="externalServices"><see cref="IExternalServicesService"/> for working with external services.</param>
    internal ExternalServiceQueries(IExternalServicesService externalServices)
    {
        _externalServices = externalServices;
    }

    /// <summary>
    /// Get all external services for an event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <returns>Collection of external service definitions.</returns>
    [HttpGet]
    public async Task<IEnumerable<ExternalServiceDefinition>> GetExternalServices([FromRoute] string eventStore)
    {
        var externalServices = await _externalServices.GetExternalServices(
            new Contracts.ExternalServices.GetExternalServicesRequest { EventStore = eventStore });

        return externalServices.Select(ToApi);
    }

    static ExternalServiceDefinition ToApi(Contracts.ExternalServices.ExternalServiceDefinition definition)
    {
        var result = new ExternalServiceDefinition
        {
            Id = definition.Id,
            Name = definition.Name,
            EndpointType = (ExternalServiceEndpointType)definition.Endpoint.Type
        };

        if (definition.Endpoint.Http is { } http)
        {
            result.Url = http.Url;
            result.AuthorizationType = GetAuthorizationType(http);
            result.Headers = http.Headers;
        }

        if (definition.Endpoint.Database is { } database)
        {
            result.Host = database.Host;
            result.Port = database.Port;
            result.Database = database.Database;
            result.Username = database.Username;
            result.Options = database.Options;
        }

        return result;
    }

    static Security.AuthorizationType GetAuthorizationType(Contracts.ExternalServices.HttpEndpointConfiguration http)
    {
        if (http.Authorization is null)
        {
            return Security.AuthorizationType.None;
        }

        if (http.Authorization.Value0 is not null)
        {
            return Security.AuthorizationType.Basic;
        }

        if (http.Authorization.Value1 is not null)
        {
            return Security.AuthorizationType.Bearer;
        }

        if (http.Authorization.Value2 is not null)
        {
            return Security.AuthorizationType.OAuth;
        }

        return Security.AuthorizationType.None;
    }
}
