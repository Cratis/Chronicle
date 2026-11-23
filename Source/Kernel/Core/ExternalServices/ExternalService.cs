// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the read model for an external service, providing query access to the definitions an event store holds.
/// </summary>
/// <param name="Id">The unique identifier of the external service.</param>
/// <param name="Name">The human-readable name of the external service.</param>
/// <param name="EndpointType">The kind of endpoint the external service exposes.</param>
/// <param name="Url">The base URL of an HTTP endpoint.</param>
/// <param name="AuthorizationType">The kind of authorization an HTTP endpoint uses.</param>
/// <param name="Headers">The HTTP header names with empty values. Queries never return header values.</param>
/// <param name="Host">The host of a database endpoint.</param>
/// <param name="Port">The port of a database endpoint, zero meaning the provider default.</param>
/// <param name="Database">The database name of a database endpoint.</param>
/// <param name="Username">The username of a database endpoint.</param>
/// <param name="Options">Additional provider-specific options a database endpoint is opened with.</param>
/// <param name="HeaderValuesSet">Whether each HTTP header has a non-empty configured value.</param>
/// <remarks>
/// Secrets - passwords, tokens, client secrets - are deliberately absent. What the workbench needs is which
/// authorization an endpoint uses, never the credential itself, and a read model that carried one would put it on
/// every wire that ever asks for the list.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.ExternalServices)]
public record ExternalService(
    string Id,
    string Name,
    Contracts.ExternalServices.ExternalServiceEndpointType EndpointType,
    string Url,
    AuthorizationType AuthorizationType,
    IDictionary<string, string> Headers,
    string Host,
    int Port,
    string Database,
    string Username,
    IDictionary<string, string> Options,
    IDictionary<string, bool> HeaderValuesSet)
{
#pragma warning disable SA1313 // Preserve the original positional record's named-argument API.
    /// <summary>
    /// Initializes the read model without header value metadata.
    /// </summary>
    /// <param name="Id">The external service identifier.</param>
    /// <param name="Name">The external service name.</param>
    /// <param name="EndpointType">The endpoint type.</param>
    /// <param name="Url">The HTTP URL.</param>
    /// <param name="AuthorizationType">The authorization type.</param>
    /// <param name="Headers">The HTTP headers.</param>
    /// <param name="Host">The database host.</param>
    /// <param name="Port">The database port.</param>
    /// <param name="Database">The database name.</param>
    /// <param name="Username">The database username.</param>
    /// <param name="Options">The database options.</param>
    public ExternalService(
        string Id,
        string Name,
        Contracts.ExternalServices.ExternalServiceEndpointType EndpointType,
        string Url,
        AuthorizationType AuthorizationType,
        IDictionary<string, string> Headers,
        string Host,
        int Port,
        string Database,
        string Username,
        IDictionary<string, string> Options)
        : this(Id, Name, EndpointType, Url, AuthorizationType, Headers, Host, Port, Database, Username, Options, new Dictionary<string, bool>())
    {
    }

    /// <summary>
    /// Deconstructs the read model without header value metadata.
    /// </summary>
    /// <param name="Id">The external service identifier.</param>
    /// <param name="Name">The external service name.</param>
    /// <param name="EndpointType">The endpoint type.</param>
    /// <param name="Url">The HTTP URL.</param>
    /// <param name="AuthorizationType">The authorization type.</param>
    /// <param name="Headers">The HTTP headers.</param>
    /// <param name="Host">The database host.</param>
    /// <param name="Port">The database port.</param>
    /// <param name="Database">The database name.</param>
    /// <param name="Username">The database username.</param>
    /// <param name="Options">The database options.</param>
    public void Deconstruct(
        out string Id,
        out string Name,
        out Contracts.ExternalServices.ExternalServiceEndpointType EndpointType,
        out string Url,
        out AuthorizationType AuthorizationType,
        out IDictionary<string, string> Headers,
        out string Host,
        out int Port,
        out string Database,
        out string Username,
        out IDictionary<string, string> Options)
    {
        Id = this.Id;
        Name = this.Name;
        EndpointType = this.EndpointType;
        Url = this.Url;
        AuthorizationType = this.AuthorizationType;
        Headers = this.Headers;
        Host = this.Host;
        Port = this.Port;
        Database = this.Database;
        Username = this.Username;
        Options = this.Options;
    }
#pragma warning restore SA1313

    /// <summary>
    /// Gets every external service registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get external services for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>A collection of external services.</returns>
    public static async Task<IEnumerable<ExternalService>> GetExternalServices(EventStoreName eventStore, IStorage storage)
    {
        var definitions = await storage.GetEventStore(eventStore).ExternalServices.GetAll();
        return definitions.ToReadModel();
    }

    /// <summary>
    /// Observes every external service registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to observe external services for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>An observable subject emitting collections of external services.</returns>
    public static ISubject<IEnumerable<ExternalService>> ObserveExternalServices(EventStoreName eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).ExternalServices.ObserveAll().TransformSubject(_ => _.ToReadModel());
}
