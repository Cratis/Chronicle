// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Auditing;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Identities;

namespace Cratis.Observation;

/// <summary>
/// Represents a handler of observers.
/// </summary>
public class ObserverHandler
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

    readonly IObserverInvoker _observerInvoker;
    readonly ICausationManager _causationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverHandler"/>.
    /// </summary>
    /// <param name="observerId">Unique identifier.</param>
    /// <param name="name">Name of the observer.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer is for.</param>
    /// <param name="observerInvoker">The actual invoker.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
    public ObserverHandler(
        ObserverId observerId,
        ObserverName name,
        EventSequenceId eventSequenceId,
        IObserverInvoker observerInvoker,
        ICausationManager causationManager)
    {
        ObserverId = observerId;
        Name = name;
        EventSequenceId = eventSequenceId;
        _observerInvoker = observerInvoker;
        _causationManager = causationManager;
    }

    /// <summary>
    /// Gets the unique identifier of the observer.
    /// </summary>
    public ObserverId ObserverId { get; }

    /// <summary>
    /// Gets the name of the observer.
    /// </summary>
    public ObserverName Name { get; }

    /// <summary>
    /// Gets the event log for the observer.
    /// </summary>
    public EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the event types for the observer.
    /// </summary>
    public IEnumerable<EventType> EventTypes => _observerInvoker.EventTypes;

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

        _causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationObserverIdProperty, ObserverId.ToString() },
            { CausationEventTypeIdProperty, metadata.Type.Id.ToString() },
            { CausationEventTypeGenerationProperty, metadata.Type.Generation.ToString() },
            { CausationEventSequenceIdProperty, EventSequenceId.ToString() },
            { CausationEventSequenceNumberProperty, metadata.SequenceNumber.ToString() }
        });

        await _observerInvoker.Invoke(content, context);

        BaseIdentityProvider.ClearCurrentIdentity();
    }
}
