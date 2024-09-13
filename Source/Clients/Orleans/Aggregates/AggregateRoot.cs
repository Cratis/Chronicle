// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1402

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/> for Orleans.
/// </summary>
public class AggregateRoot : Grain, IAggregateRoot, IAggregateRootContextHolder
{
    /// <summary>
    /// Mutation of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal AggregateRootMutation? _mutation;

    StatelessAggregateRootMutator? _mutator;
    IAggregateRootEventHandlers? _eventHandlers;
    IEventStore? _eventStore;
    IEventLog? _eventLog;
    IEventSerializer? _eventSerializer;
    ICorrelationIdAccessor? _correlationIdAccessor;

    /// <inheritdoc/>
    public IAggregateRootContext? Context { get; set; }

    /// <summary>
    /// Gets a value indicating whether the aggregate root is new.
    /// </summary>
    protected bool IsNew => !Context?.HasEvents ?? true;

    /// <inheritdoc/>
    public async Task SetContext(IAggregateRootContext context)
    {
        Context = context;
        _mutator = new StatelessAggregateRootMutator(
            context,
            _eventStore!,
            _eventSerializer!,
            _eventHandlers!,
            _correlationIdAccessor!);
        _mutation = new AggregateRootMutation(context, _mutator, _eventLog!);

        await _mutator.Rehydrate();
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStore = ServiceProvider.GetRequiredService<IEventStore>();
        _eventLog = ServiceProvider.GetRequiredService<IEventLog>();
        _eventSerializer = ServiceProvider.GetRequiredService<IEventSerializer>();
        _correlationIdAccessor = ServiceProvider.GetRequiredService<ICorrelationIdAccessor>();

        var eventHandlersFactory = ServiceProvider.GetRequiredService<IAggregateRootEventHandlersFactory>();
        _eventHandlers = eventHandlersFactory.GetFor(this);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Apply(object @event)
    {
        return _mutation?.Apply(@event) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<AggregateRootCommitResult> Commit() => _mutation!.Commit();

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/> for stateful Orleans based Grains.
/// </summary>
/// <typeparam name="TState">Type of state for the grain.</typeparam>
public class AggregateRoot<TState> : Grain, IAggregateRoot, IAggregateRootContextHolder
{
    /// <summary>
    /// Mutation of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal AggregateRootMutation? _mutation;

    /// <summary>
    /// State of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal AggregateRootState<TState>? _state;

    StatefulAggregateRootMutator<TState>? _mutator;
    IEventLog? _eventLog;
    IAggregateRootStateProviders? _stateProviders;

    /// <inheritdoc/>
    public IAggregateRootContext? Context { get; set; }

    /// <summary>
    /// Gets the current state of the aggregate root.
    /// </summary>
    public TState State => _state!.State;

    /// <summary>
    /// Gets a value indicating whether the aggregate root is new.
    /// </summary>
    protected bool IsNew => Context?.NextSequenceNumber is null || Context?.NextSequenceNumber != EventSequenceNumber.First;

    /// <inheritdoc/>
    public async Task SetContext(IAggregateRootContext context)
    {
        Context = context;
        var stateProvider = await _stateProviders!.CreateFor<TState>(context!);
        _state = new AggregateRootState<TState>();
        _mutator = new StatefulAggregateRootMutator<TState>(_state, stateProvider);
        _mutation = new AggregateRootMutation(context, _mutator, _eventLog!);

        await _mutator.Rehydrate();
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventLog = ServiceProvider.GetRequiredService<IEventLog>();
        _stateProviders = ServiceProvider.GetRequiredService<IAggregateRootStateProviders>();

        await OnActivate();
    }

    /// <inheritdoc/>
    public Task Apply(object @event)
    {
        return _mutation?.Apply(@event) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<AggregateRootCommitResult> Commit() => _mutation!.Commit();

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}
