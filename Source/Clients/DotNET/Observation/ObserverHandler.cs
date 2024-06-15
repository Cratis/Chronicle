// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a handler of observers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverHandler"/>.
/// </remarks>
/// <param name="observerId">Unique identifier.</param>
/// <param name="name">Name of the observer.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer is for.</param>
/// <param name="observerInvoker">The actual invoker.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
public class ObserverHandler(
    ObserverId observerId,
    ObserverName name,
    EventSequenceId eventSequenceId,
    IObserverInvoker observerInvoker,
    ICausationManager causationManager)
{
    /// <summary>
    /// The observer id causation property.
    /// </summary>
    public const string CausationObserverIdProperty = "ObserverId";

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
    /// The causation type for client observer.
    /// </summary>
    public static readonly CausationType CausationType = new("Client Observer");

    /// <summary>
    /// Gets the unique identifier of the observer.
    /// </summary>
    public ObserverId ObserverId { get; } = observerId;

    /// <summary>
    /// Gets the name of the observer.
    /// </summary>
    public ObserverName Name { get; } = name;

    /// <summary>
    /// Gets the event log for the observer.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequenceId;

    /// <summary>
    /// Gets the event types for the observer.
    /// </summary>
    public IEnumerable<EventType> EventTypes => observerInvoker.EventTypes;

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
            { CausationObserverIdProperty, ObserverId.ToString() },
            { CausationEventTypeIdProperty, metadata.Type.Id.ToString() },
            { CausationEventTypeGenerationProperty, metadata.Type.Generation.ToString() },
            { CausationEventSequenceIdProperty, EventSequenceId.ToString() },
            { CausationEventSequenceNumberProperty, metadata.SequenceNumber.ToString() }
        });

        await observerInvoker.Invoke(content, context);

        BaseIdentityProvider.ClearCurrentIdentity();
    }
}
