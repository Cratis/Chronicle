// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
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
/// <param name="Headers">Additional headers an HTTP endpoint is called with.</param>
/// <param name="Host">The host of a database endpoint.</param>
/// <param name="Port">The port of a database endpoint, zero meaning the provider default.</param>
/// <param name="Database">The database name of a database endpoint.</param>
/// <param name="Username">The username of a database endpoint.</param>
/// <param name="Options">Additional provider-specific options a database endpoint is opened with.</param>
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
    IDictionary<string, string> Options)
{
    /// <summary>
    /// Gets every external service registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get external services for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>A collection of external services.</returns>
    internal static async Task<IEnumerable<ExternalService>> GetExternalServices(string eventStore, IStorage storage)
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
    internal static ISubject<IEnumerable<ExternalService>> ObserveExternalServices(string eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).ExternalServices.ObserveAll().TransformSubject(_ => _.ToReadModel());
}
