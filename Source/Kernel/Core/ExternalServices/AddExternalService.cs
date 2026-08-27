// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the command for registering a single external service from its individual settings.
/// </summary>
/// <param name="EventStore">The event store the external service belongs to.</param>
/// <param name="Id">The unique identifier, defaulting to the name when left empty.</param>
/// <param name="Name">The human-readable name.</param>
/// <param name="EndpointType">The kind of endpoint the external service exposes.</param>
/// <param name="Url">The base URL of an HTTP endpoint.</param>
/// <param name="AuthorizationType">Which authorization an HTTP endpoint uses.</param>
/// <param name="BasicUsername">The username for basic authorization.</param>
/// <param name="BasicPassword">The password for basic authorization.</param>
/// <param name="BearerToken">The token for bearer authorization.</param>
/// <param name="OAuthAuthority">The authority for OAuth authorization.</param>
/// <param name="OAuthClientId">The client identifier for OAuth authorization.</param>
/// <param name="OAuthClientSecret">The client secret for OAuth authorization.</param>
/// <param name="Headers">Additional headers an HTTP endpoint is called with.</param>
/// <param name="Host">The host of a database endpoint.</param>
/// <param name="Port">The port of a database endpoint, zero meaning the provider default.</param>
/// <param name="Database">The database name of a database endpoint.</param>
/// <param name="Username">The username of a database endpoint.</param>
/// <param name="Password">The password of a database endpoint.</param>
/// <param name="Options">Additional provider-specific options a database endpoint is opened with.</param>
/// <remarks>
/// This is the shape a form fills in: one endpoint, its settings spread flat, and the authorization chosen by
/// kind rather than supplied as one of several shapes. <see cref="AddExternalServices"/> is the shape a client
/// registering what it already knows sends.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.ExternalServices)]
public record AddExternalService(
    EventStoreName EventStore,
    Concepts.ExternalServices.ExternalServiceId Id,
    string Name,
    ExternalServiceEndpointType EndpointType,
    string Url,
    AuthorizationType AuthorizationType,
    string BasicUsername,
    string BasicPassword,
    string BearerToken,
    string OAuthAuthority,
    string OAuthClientId,
    string OAuthClientSecret,
    IDictionary<string, string> Headers,
    string Host,
    int Port,
    string Database,
    string Username,
    string Password,
    IDictionary<string, string> Options)
{
    /// <summary>
    /// Handles the command by saving the assembled definition into the event store's external service storage.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IStorage storage) =>
        storage.GetEventStore(EventStore).ExternalServices.Save(
            new Contracts.ExternalServices.ExternalServiceDefinition
            {
                Id = Id == Concepts.ExternalServices.ExternalServiceId.Unspecified ? Name : Id,
                Name = Name,
                Endpoint = Endpoint()
            }.ToKernel());

    ExternalServiceEndpoint Endpoint() =>
        EndpointType == ExternalServiceEndpointType.Http
            ? new() { Type = EndpointType, Http = Http() }
            : new() { Type = EndpointType, Database = DatabaseEndpoint() };

    HttpEndpointConfiguration Http() =>
        new()
        {
            Url = Url,
            Authorization = Authorization(),
            Headers = Headers.ToDictionary(_ => _.Key, _ => _.Value)
        };

    DatabaseEndpointConfiguration DatabaseEndpoint() =>
        new()
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            Options = Options.ToDictionary(_ => _.Key, _ => _.Value)
        };

    Contracts.Primitives.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? Authorization() =>
        AuthorizationType switch
        {
            AuthorizationType.Basic => new(new BasicAuthorization { Username = BasicUsername, Password = BasicPassword }),
            AuthorizationType.Bearer => new(new BearerTokenAuthorization { Token = BearerToken }),
            AuthorizationType.OAuth => new(new OAuthAuthorization { Authority = OAuthAuthority, ClientId = OAuthClientId, ClientSecret = OAuthClientSecret }),
            _ => null
        };
}
