// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
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
/// <param name="Headers">The header names with empty values. Queries never return header values.</param>
/// <param name="IsReplayable">Whether the webhook can be replayed.</param>
/// <param name="IsActive">Whether the webhook is delivering.</param>
/// <param name="GenerationDelivery">The policy for delivering event generations.</param>
/// <param name="Signing">The effective signing policy.</param>
/// <param name="HeaderValuesSet">Whether each header has a non-empty configured value.</param>
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
    bool IsActive,
    EventGenerationDelivery GenerationDelivery,
    Contracts.Observation.Webhooks.WebhookSigningMode Signing,
    IDictionary<string, bool> HeaderValuesSet)
{
    /// <summary>
    /// Initializes a webhook read model using the original Compatibility delivery policy.
    /// </summary>
    /// <param name="Id">The identity of the webhook.</param>
    /// <param name="Identifier">The unique identifier of the webhook.</param>
    /// <param name="Url">Where the webhook is called.</param>
    /// <param name="EventSequenceId">The event sequence the webhook observes.</param>
    /// <param name="EventTypes">The event types the webhook is called for.</param>
    /// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
    /// <param name="Headers">Additional headers the call carries.</param>
    /// <param name="IsReplayable">Whether the webhook can be replayed.</param>
    /// <param name="IsActive">Whether the webhook is delivering.</param>
#pragma warning disable SA1313 // Preserve the original positional record's named-argument API.
    public WebhookDetails(
        string Id,
        string Identifier,
        string Url,
        string EventSequenceId,
        IEnumerable<Contracts.Events.EventType> EventTypes,
        AuthorizationType AuthorizationType,
        IDictionary<string, string> Headers,
        bool IsReplayable,
        bool IsActive)
        : this(Id, Identifier, Url, EventSequenceId, EventTypes, AuthorizationType, Headers, IsReplayable, IsActive, EventGenerationDelivery.Compatibility)
    {
    }

    /// <summary>
    /// Initializes a webhook read model using the original unsigned delivery policy.
    /// </summary>
    /// <param name="Id">The identity of the webhook.</param>
    /// <param name="Identifier">The unique identifier of the webhook.</param>
    /// <param name="Url">Where the webhook is called.</param>
    /// <param name="EventSequenceId">The event sequence the webhook observes.</param>
    /// <param name="EventTypes">The event types the webhook is called for.</param>
    /// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
    /// <param name="Headers">Additional headers the call carries.</param>
    /// <param name="IsReplayable">Whether the webhook can be replayed.</param>
    /// <param name="IsActive">Whether the webhook is delivering.</param>
    /// <param name="GenerationDelivery">The policy for delivering event generations.</param>
    public WebhookDetails(
        string Id,
        string Identifier,
        string Url,
        string EventSequenceId,
        IEnumerable<Contracts.Events.EventType> EventTypes,
        AuthorizationType AuthorizationType,
        IDictionary<string, string> Headers,
        bool IsReplayable,
        bool IsActive,
        EventGenerationDelivery GenerationDelivery)
        : this(Id, Identifier, Url, EventSequenceId, EventTypes, AuthorizationType, Headers, IsReplayable, IsActive, GenerationDelivery, Contracts.Observation.Webhooks.WebhookSigningMode.Disabled)
    {
    }

    /// <summary>
    /// Initializes a webhook read model without header value metadata.
    /// </summary>
    /// <param name="Id">The identity of the webhook.</param>
    /// <param name="Identifier">The unique identifier of the webhook.</param>
    /// <param name="Url">Where the webhook is called.</param>
    /// <param name="EventSequenceId">The event sequence the webhook observes.</param>
    /// <param name="EventTypes">The event types the webhook is called for.</param>
    /// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
    /// <param name="Headers">Additional headers the call carries.</param>
    /// <param name="IsReplayable">Whether the webhook can be replayed.</param>
    /// <param name="IsActive">Whether the webhook is delivering.</param>
    /// <param name="GenerationDelivery">The policy for delivering event generations.</param>
    /// <param name="Signing">The effective signing policy.</param>
    public WebhookDetails(
        string Id,
        string Identifier,
        string Url,
        string EventSequenceId,
        IEnumerable<Contracts.Events.EventType> EventTypes,
        AuthorizationType AuthorizationType,
        IDictionary<string, string> Headers,
        bool IsReplayable,
        bool IsActive,
        EventGenerationDelivery GenerationDelivery = EventGenerationDelivery.Compatibility,
        Contracts.Observation.Webhooks.WebhookSigningMode Signing = Contracts.Observation.Webhooks.WebhookSigningMode.Disabled)
        : this(Id, Identifier, Url, EventSequenceId, EventTypes, AuthorizationType, Headers, IsReplayable, IsActive, GenerationDelivery, Signing, new Dictionary<string, bool>())
    {
    }

    /// <summary>
    /// Gets every webhook registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get webhooks for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the definitions.</param>
    /// <returns>A collection of webhooks.</returns>
    public static async Task<IEnumerable<WebhookDetails>> GetWebhooks(EventStoreName eventStore, IStorage storage)
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
    public static ISubject<IEnumerable<WebhookDetails>> ObserveWebhooks(EventStoreName eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).Webhooks.ObserveAll().TransformSubject(_ => _.ToReadModel());

    /// <summary>
    /// Deconstructs the original nine-field webhook read model shape.
    /// </summary>
    /// <param name="Id">The identity of the webhook.</param>
    /// <param name="Identifier">The unique identifier of the webhook.</param>
    /// <param name="Url">Where the webhook is called.</param>
    /// <param name="EventSequenceId">The event sequence the webhook observes.</param>
    /// <param name="EventTypes">The event types the webhook is called for.</param>
    /// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
    /// <param name="Headers">Additional headers the call carries.</param>
    /// <param name="IsReplayable">Whether the webhook can be replayed.</param>
    /// <param name="IsActive">Whether the webhook is delivering.</param>
    public void Deconstruct(
        out string Id,
        out string Identifier,
        out string Url,
        out string EventSequenceId,
        out IEnumerable<Contracts.Events.EventType> EventTypes,
        out AuthorizationType AuthorizationType,
        out IDictionary<string, string> Headers,
        out bool IsReplayable,
        out bool IsActive) =>
        (Id, Identifier, Url, EventSequenceId, EventTypes, AuthorizationType, Headers, IsReplayable, IsActive) =
        (this.Id, this.Identifier, this.Url, this.EventSequenceId, this.EventTypes, this.AuthorizationType, this.Headers, this.IsReplayable, this.IsActive);

    /// <summary>
    /// Deconstructs the ten-field webhook read model shape including generation delivery.
    /// </summary>
    /// <param name="Id">The identity of the webhook.</param>
    /// <param name="Identifier">The unique identifier of the webhook.</param>
    /// <param name="Url">Where the webhook is called.</param>
    /// <param name="EventSequenceId">The event sequence the webhook observes.</param>
    /// <param name="EventTypes">The event types the webhook is called for.</param>
    /// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
    /// <param name="Headers">Additional headers the call carries.</param>
    /// <param name="IsReplayable">Whether the webhook can be replayed.</param>
    /// <param name="IsActive">Whether the webhook is delivering.</param>
    /// <param name="GenerationDelivery">The policy for delivering event generations.</param>
    public void Deconstruct(
        out string Id,
        out string Identifier,
        out string Url,
        out string EventSequenceId,
        out IEnumerable<Contracts.Events.EventType> EventTypes,
        out AuthorizationType AuthorizationType,
        out IDictionary<string, string> Headers,
        out bool IsReplayable,
        out bool IsActive,
        out EventGenerationDelivery GenerationDelivery) =>
        (Id, Identifier, Url, EventSequenceId, EventTypes, AuthorizationType, Headers, IsReplayable, IsActive, GenerationDelivery) =
        (this.Id, this.Identifier, this.Url, this.EventSequenceId, this.EventTypes, this.AuthorizationType, this.Headers, this.IsReplayable, this.IsActive, this.GenerationDelivery);

    /// <summary>
    /// Deconstructs the read model without header value metadata.
    /// </summary>
    /// <param name="Id">The identity of the webhook.</param>
    /// <param name="Identifier">The unique identifier of the webhook.</param>
    /// <param name="Url">Where the webhook is called.</param>
    /// <param name="EventSequenceId">The event sequence the webhook observes.</param>
    /// <param name="EventTypes">The event types the webhook is called for.</param>
    /// <param name="AuthorizationType">Which kind of authorization the call carries.</param>
    /// <param name="Headers">Additional headers the call carries.</param>
    /// <param name="IsReplayable">Whether the webhook can be replayed.</param>
    /// <param name="IsActive">Whether the webhook is delivering.</param>
    /// <param name="GenerationDelivery">The policy for delivering event generations.</param>
    /// <param name="Signing">The effective signing policy.</param>
    public void Deconstruct(
        out string Id,
        out string Identifier,
        out string Url,
        out string EventSequenceId,
        out IEnumerable<Contracts.Events.EventType> EventTypes,
        out AuthorizationType AuthorizationType,
        out IDictionary<string, string> Headers,
        out bool IsReplayable,
        out bool IsActive,
        out EventGenerationDelivery GenerationDelivery,
        out Contracts.Observation.Webhooks.WebhookSigningMode Signing)
    {
        Deconstruct(out Id, out Identifier, out Url, out EventSequenceId, out EventTypes, out AuthorizationType, out Headers, out IsReplayable, out IsActive, out GenerationDelivery);
        Signing = this.Signing;
    }
#pragma warning restore SA1313
}
