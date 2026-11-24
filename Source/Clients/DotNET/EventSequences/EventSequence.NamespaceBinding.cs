// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Transactions;
using Grpc.Core;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    NamespaceBinding? _namespaceBinding;

    /// <summary>Creates a separate writer pinned to the supplied namespace snapshot.</summary>
    /// <param name="binding">The open binding for this writer's event store and namespace.</param>
    /// <returns>A writer that retains the captured namespace identity for every append.</returns>
    /// <exception cref="NamespaceBindingNotValid">The binding does not match this writer's target or existing identity.</exception>
    internal EventSequence BindNamespace(NamespaceBinding binding)
    {
        binding.RequireFor(eventStoreName, @namespace);
        if (_namespaceBinding is not null && _namespaceBinding != binding) throw new NamespaceBindingNotValid();
        return new(
            eventStoreName,
            @namespace,
            eventSequenceId,
            connection,
            eventTypes,
            constraints,
            eventSerializer,
            correlationIdAccessor,
            concurrencyScopeStrategies,
            causationManager,
            unitOfWorkManager,
            identityProvider,
            jsonSerializerOptions,
            activitySource,
            sideEffectHandlers,
            eventSources,
            telemetry,
            tagResolver)
        {
            _namespaceBinding = binding,
            _appendNotifications = _appendNotifications
        };
    }

    /// <summary>Pins a transactional append to this writer's captured namespace identity.</summary>
    /// <param name="unitOfWork">The unit of work that will stage the append.</param>
    /// <exception cref="NamespaceBindingsNotSupported">The unit of work cannot retain a namespace binding.</exception>
    internal void BindNamespaceTo(IUnitOfWork unitOfWork)
    {
        if (_namespaceBinding is null) return;
        if (unitOfWork is not INamespaceBoundUnitOfWork supported) throw new NamespaceBindingsNotSupported();
        supported.BindNamespace(_namespaceBinding);
    }

    static async Task<TResult> SendNamespaceBound<TResult>(Func<Task<TResult>> send)
    {
        try
        {
            return await send();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Unimplemented)
        {
            throw new NamespaceBindingsNotSupported();
        }
    }

    static Contracts.Sequences.EventForEventSourceIdWithNamedTags[] ToBoundEvents(
        List<Contracts.Sequences.EventForEventSourceId> events,
        IReadOnlyList<NamedTag>[] namedTags) =>
        events.Select((@event, index) => new Contracts.Sequences.EventForEventSourceIdWithNamedTags
        {
            EventSourceId = @event.EventSourceId,
            EventSourceType = @event.EventSourceType,
            EventStreamType = @event.EventStreamType,
            EventStreamId = @event.EventStreamId,
            EventType = @event.EventType,
            Content = @event.Content,
            Tags = @event.Tags,
            NamedTags = namedTags[index].Select(tag => tag.ToSequencesContract()).ToArray(),
            Occurred = @event.Occurred,
            Subject = @event.Subject,
            Causation = @event.Causation,
            EventSource = @event.EventSource
        }).ToArray();

    Task<Contracts.Sequences.AppendResponse> AppendToBoundNamespace(
        Contracts.Sequences.AppendRequest request,
        IReadOnlyList<NamedTag> namedTags) =>
        SendNamespaceBound(() => _servicesAccessor.Services.Sequences.AppendToNamespace(new Contracts.Sequences.AppendToNamespaceRequest
        {
            EventStore = request.EventStore,
            Namespace = request.Namespace,
            ExpectedIncarnation = _namespaceBinding!.Incarnation!.Value,
            EventSequenceId = request.EventSequenceId,
            EventSourceType = request.EventSourceType,
            EventSourceId = request.EventSourceId,
            EventStreamType = request.EventStreamType,
            EventStreamId = request.EventStreamId,
            CorrelationId = request.CorrelationId,
            EventType = request.EventType,
            Content = request.Content,
            Causation = request.Causation,
            CausedBy = request.CausedBy,
            Tags = request.Tags,
            NamedTags = namedTags.Select(tag => tag.ToSequencesContract()).ToArray(),
            ConcurrencyScope = request.ConcurrencyScope,
            Occurred = request.Occurred,
            Subject = request.Subject,
            EventSource = request.EventSource,
            EffectReceipt = request.EffectReceipt
        }).EnsureSuccess());

    Task<Contracts.Sequences.AppendOnceResponse> AppendOnceToBoundNamespace(
        Contracts.Sequences.AppendRequest request,
        IdempotencyKey key,
        IReadOnlyList<NamedTag> namedTags) =>
        SendNamespaceBound(() => _servicesAccessor.Services.Sequences.AppendOnceToNamespace(new Contracts.Sequences.AppendOnceToNamespaceRequest
        {
            EventStore = request.EventStore,
            Namespace = request.Namespace,
            ExpectedIncarnation = _namespaceBinding!.Incarnation!.Value,
            EventSequenceId = request.EventSequenceId,
            EventSourceType = request.EventSourceType,
            EventSourceId = request.EventSourceId,
            EventStreamType = request.EventStreamType,
            EventStreamId = request.EventStreamId,
            CorrelationId = request.CorrelationId,
            EventType = request.EventType,
            Content = request.Content,
            Causation = request.Causation,
            CausedBy = request.CausedBy,
            Tags = request.Tags,
            NamedTags = namedTags.Select(tag => tag.ToSequencesContract()).ToArray(),
            ConcurrencyScope = request.ConcurrencyScope,
            Occurred = request.Occurred,
            Subject = request.Subject,
            EventSource = request.EventSource,
            IdempotencyKey = key.Value
        }).EnsureSuccess());

    Task<Contracts.Sequences.AppendManyResponse> AppendManyToBoundNamespace(
        Contracts.Sequences.AppendManyForEventSourcesRequest request,
        List<Contracts.Sequences.EventForEventSourceId> events,
        IReadOnlyList<NamedTag>[] namedTags) =>
        SendNamespaceBound(() => _servicesAccessor.Services.Sequences.AppendManyForEventSourcesToNamespace(new Contracts.Sequences.AppendManyForEventSourcesToNamespaceRequest
        {
            EventStore = request.EventStore,
            Namespace = request.Namespace,
            ExpectedIncarnation = _namespaceBinding!.Incarnation!.Value,
            EventSequenceId = request.EventSequenceId,
            Events = ToBoundEvents(events, namedTags),
            CorrelationId = request.CorrelationId,
            Tags = request.Tags,
            Causation = request.Causation,
            CausedBy = request.CausedBy,
            ConcurrencyScopes = request.ConcurrencyScopes,
            EffectReceipt = request.EffectReceipt
        }).EnsureSuccess());

    async Task<AppendManyResult> AppendManyOnceToBoundNamespace(
        Contracts.Sequences.AppendManyForEventSourcesRequest request,
        List<Contracts.Sequences.EventForEventSourceId> events,
        IReadOnlyList<NamedTag>[] namedTags,
        IdempotencyKey?[] keys)
    {
        var response = await SendNamespaceBound(() => _servicesAccessor.Services.Sequences.AppendManyOnceToNamespace(new Contracts.Sequences.AppendManyOnceToNamespaceRequest
        {
            EventStore = request.EventStore,
            Namespace = request.Namespace,
            ExpectedIncarnation = _namespaceBinding!.Incarnation!.Value,
            EventSequenceId = request.EventSequenceId,
            Events = ToBoundEvents(events, namedTags).Select((@event, index) => new Contracts.Sequences.KeyedEventForEventSourceIdWithNamedTags
            {
                Event = @event,
                IdempotencyKey = keys[index]?.Value ?? string.Empty
            }).ToArray(),
            CorrelationId = request.CorrelationId,
            Tags = request.Tags,
            Causation = request.Causation,
            CausedBy = request.CausedBy,
            ConcurrencyScopes = request.ConcurrencyScopes
        }).EnsureSuccess());
        return response.ToClient();
    }
}
