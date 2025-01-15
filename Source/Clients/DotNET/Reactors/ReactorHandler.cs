// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a handler of Reactors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorHandler"/>.
/// </remarks>
/// <param name="reactorId">Unique identifier.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the Reactor is for.</param>
/// <param name="reactorInvoker">The actual invoker.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
public class ReactorHandler(
    ReactorId reactorId,
    EventSequenceId eventSequenceId,
    IReactorInvoker reactorInvoker,
    ICausationManager causationManager) : IDisposable
{
    /// <summary>
    /// The Reactor id causation property.
    /// </summary>
    public const string CausationReactorIdProperty = "ReactorId";

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
    /// The causation type for client Reactor.
    /// </summary>
    public static readonly CausationType CausationType = new("Client Reactor");

    readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Gets the unique identifier of the Reactor.
    /// </summary>
    public ReactorId Id { get; } = reactorId;

    /// <summary>
    /// Gets the event log for the Reactor.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequenceId;

    /// <summary>
    /// Gets the event types for the Reactor.
    /// </summary>
    public IEnumerable<EventType> EventTypes => reactorInvoker.EventTypes;

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> for the handler.
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>
    /// Handle next event.
    /// </summary>
    /// <param name="metadata"><see cref="EventMetadata"/> for the event.</param>
    /// <param name="context"><see cref="EventContext"/> for the event.</param>
    /// <param name="content">Actual content.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for creating the actual instance of the reactor.</param>
    /// <returns>Awaitable task.</returns>
    public async Task OnNext(EventMetadata metadata, EventContext context, object content, IServiceProvider serviceProvider)
    {
        BaseIdentityProvider.SetCurrentIdentity(Identity.System with { OnBehalfOf = context.CausedBy });

        causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationReactorIdProperty, Id.ToString() },
            { CausationEventTypeIdProperty, metadata.Type.Id.ToString() },
            { CausationEventTypeGenerationProperty, metadata.Type.Generation.ToString() },
            { CausationEventSequenceIdProperty, EventSequenceId.ToString() },
            { CausationEventSequenceNumberProperty, metadata.SequenceNumber.ToString() }
        });

        await reactorInvoker.Invoke(serviceProvider, content, context);

        BaseIdentityProvider.ClearCurrentIdentity();
    }

    /// <summary>
    /// Disconnect the handler.
    /// </summary>
    public void Disconnect() => _cancellationTokenSource.Cancel();

    /// <inheritdoc/>
    public void Dispose() => _cancellationTokenSource.Dispose();
}
