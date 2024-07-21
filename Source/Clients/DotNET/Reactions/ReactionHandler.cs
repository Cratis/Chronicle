// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Represents a handler of reactions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactionHandler"/>.
/// </remarks>
/// <param name="reactionId">Unique identifier.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the reaction is for.</param>
/// <param name="reactionInvoker">The actual invoker.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
public class ReactionHandler(
    ReactionId reactionId,
    EventSequenceId eventSequenceId,
    IReactionInvoker reactionInvoker,
    ICausationManager causationManager)
{
    /// <summary>
    /// The reaction id causation property.
    /// </summary>
    public const string CausationReactionIdProperty = "ReactionId";

    /// <summary>
    /// The event type causation property.
    /// </summary>
    public const string CausationEventTypeIdProperty = "eventTypeId";

    /// <summary>
    /// The event type generation causation property.
    /// </summary>
    public const string CausationEventTypeGenerationProperty = "eventTypeGeneration";

    /// <summary>
    /// The event sequence id causation property.
    /// </summary>
    public const string CausationEventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The event sequence number causation property.
    /// </summary>
    public const string CausationEventSequenceNumberProperty = "eventSequenceNumber";

    /// <summary>
    /// The causation type for client reaction.
    /// </summary>
    public static readonly CausationType CausationType = new("Client Reaction");

    /// <summary>
    /// Gets the unique identifier of the reaction.
    /// </summary>
    public ReactionId Id { get; } = reactionId;

    /// <summary>
    /// Gets the event log for the reaction.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequenceId;

    /// <summary>
    /// Gets the event types for the reaction.
    /// </summary>
    public IEnumerable<EventType> EventTypes => reactionInvoker.EventTypes;

    /// <summary>
    /// Handle next event.
    /// </summary>
    /// <param name="metadata"><see cref="EventMetadata"/> for the event.</param>
    /// <param name="context"><see cref="EventContext"/> for the event.</param>
    /// <param name="content">Actual content.</param>
    /// <returns>Awaitable task.</returns>
    public async Task OnNext(EventMetadata metadata, EventContext context, object content)
    {
        BaseIdentityProvider.SetCurrentIdentity(Identity.System with { OnBehalfOf = context.CausedBy });

        causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationReactionIdProperty, Id.ToString() },
            { CausationEventTypeIdProperty, metadata.Type.Id.ToString() },
            { CausationEventTypeGenerationProperty, metadata.Type.Generation.ToString() },
            { CausationEventSequenceIdProperty, EventSequenceId.ToString() },
            { CausationEventSequenceNumberProperty, metadata.SequenceNumber.ToString() }
        });

        await reactionInvoker.Invoke(content, context);

        BaseIdentityProvider.ClearCurrentIdentity();
    }
}
