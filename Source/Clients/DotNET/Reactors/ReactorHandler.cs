// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents a handler of Reactors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorHandler"/>.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the Reactors belong to.</param>
/// <param name="reactorId">Unique identifier.</param>
/// <param name="reactorType">The type of the reactor.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the Reactor is for.</param>
/// <param name="reactorInvoker">The actual invoker.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for working with causation.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for managing identity context.</param>
public class ReactorHandler(
    IEventStore eventStore,
    ReactorId reactorId,
    Type reactorType,
    EventSequenceId eventSequenceId,
    IReactorInvoker reactorInvoker,
    ICausationManager causationManager,
    IIdentityProvider identityProvider) : IDisposable, IReactorHandler
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

    /// <inheritdoc/>
    public ReactorId Id { get; } = reactorId;

    /// <inheritdoc/>
    public Type ReactorType { get; } = reactorType;

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; } = eventSequenceId;

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => reactorInvoker.EventTypes;

    /// <inheritdoc/>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> CreateReactorInstance(IServiceProvider serviceProvider) =>
        reactorInvoker.CreateInstance(serviceProvider);

    /// <inheritdoc/>
    public async Task OnNext(EventContext context, object content, object reactorInstance)
    {
        identityProvider.SetCurrentIdentity(Identity.System with { OnBehalfOf = context.CausedBy });

        causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationReactorIdProperty, Id.ToString() },
            { CausationEventTypeIdProperty, context.EventType.Id.ToString() },
            { CausationEventTypeGenerationProperty, context.EventType.Generation.ToString() },
            { CausationEventSequenceIdProperty, EventSequenceId.ToString() },
            { CausationEventSequenceNumberProperty, context.SequenceNumber.ToString() }
        });

        await reactorInvoker.Invoke(reactorInstance, content, context);

        identityProvider.ClearCurrentIdentity();
    }

    /// <summary>
    /// Get the current state of the Reactor.
    /// </summary>
    /// <returns>Current <see cref="ReactorState"/>.</returns>
    public async Task<ReactorState> GetState()
    {
        var request = new Contracts.Observation.GetObserverInformationRequest
        {
            ObserverId = Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId
        };
        var servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        var state = await servicesAccessor.Services.Observers.GetObserverInformation(request);
        return new ReactorState(
            Id,
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }

    /// <summary>
    /// Get any failed partitions for a specific reactor.
    /// </summary>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    public Task<IEnumerable<FailedPartition>> GetFailedPartitions() =>
        eventStore.FailedPartitions.GetFailedPartitionsFor(Id);

    /// <summary>
    /// Disconnect the handler.
    /// </summary>
    public void Disconnect() => _cancellationTokenSource.Cancel();

    /// <inheritdoc/>
    public void Dispose() => _cancellationTokenSource.Dispose();
}
