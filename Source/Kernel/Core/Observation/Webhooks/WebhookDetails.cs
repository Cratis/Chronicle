// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the read model for a webhook, providing query access to the definitions an event store holds.
/// </summary>
/// <param name="Id">The identity of the webhook, which is its identifier.</param>
/// <param name="Identifier">The unique identifier of the webhook.</param>
/// <param name="Url">Where the webhook is called.</param>
/// <param name="EventSequenceId">The event sequence the webhook observes.</param>
/// <param name="EventTypes">The event types the webhook is called for.</param>
/// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
/// <param name="Headers">Additional headers the call carries.</param>
/// <param name="IsReplayable">Whether the webhook can be replayed.</param>
/// <param name="IsActive">Whether the webhook is delivering.</param>
/// <remarks>
/// The authorization is reported as its <see cref="Contracts.Security.AuthorizationType"/> only. The credentials
/// themselves are stored encrypted and never leave the kernel - a caller listing webhooks wants to know how one
/// authenticates, not what it authenticates with.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Webhooks)]
public record WebhookDetails(
    string Id,
    string Identifier,
    string Url,
    string EventSequenceId,
    IEnumerable<Contracts.Events.EventType> EventTypes,
    AuthorizationType AuthorizationType,
    IDictionary<string, string> Headers,
    bool IsReplayable,
    bool IsActive)
{
    /// <summary>
    /// Gets every webhook registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get webhooks for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>A collection of webhooks.</returns>
    internal static async Task<IEnumerable<WebhookDetails>> GetWebhooks(EventStoreName eventStore, IStorage storage)
    {
        var definitions = await storage.GetEventStore(eventStore).Webhooks.GetAll();
        return definitions.ToReadModel();
    }

    /// <summary>
    /// Observes every webhook registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to observe webhooks for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>An observable subject emitting collections of webhooks.</returns>
    internal static ISubject<IEnumerable<WebhookDetails>> ObserveWebhooks(EventStoreName eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).Webhooks.ObserveAll().TransformSubject(_ => _.ToReadModel());
}
