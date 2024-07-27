// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1402

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/> for Orleans.
/// </summary>
public class AggregateRoot : Grain, IAggregateRoot
{
    StatelessAggregateRootMutator? _mutator;
    IAggregateRootContext? _context;
    AggregateRootMutation? _mutation;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var eventStore = ServiceProvider.GetRequiredService<IEventStore>();
        var eventLog = ServiceProvider.GetRequiredService<IEventLog>();
        var eventSerializer = ServiceProvider.GetRequiredService<IEventSerializer>();

        // TODO: Fix CorrelationId to be a real value from the current context
        _context = new AggregateRootContext(
            CorrelationId.New(),
            this.GetPrimaryKeyString(),
            eventLog,
            this,
            true);

        var eventHandlersFactory = ServiceProvider.GetRequiredService<IAggregateRootEventHandlersFactory>();
        var eventHandlers = eventHandlersFactory.GetFor(this);

        _mutator = new StatelessAggregateRootMutator(
            _context,
            eventStore,
            eventSerializer,
            eventHandlers);
        _mutation = new AggregateRootMutation(_context, _mutator, eventLog, ServiceProvider.GetRequiredService<ICausationManager>());

        await _mutator.Rehydrate();

        await OnActivate();
    }

    /// <inheritdoc/>
    public Task Apply<T>(T @event)
        where T : class
    {
        return _mutation?.Apply(@event) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<AggregateRootCommitResult> Commit()
    {
        return _mutation?.Commit() ?? Task.FromResult(AggregateRootCommitResult.Failed(ImmutableList<object>.Empty));
    }

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
public class AggregateRoot<TState> : Grain, IAggregateRoot
{
    StatefulAggregateRootMutator<TState>? _mutator;
    IAggregateRootContext? _context;
    AggregateRootMutation? _mutation;
    AggregateRootState<TState>? _state;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var eventLog = ServiceProvider.GetRequiredService<IEventLog>();

        // TODO: Fix CorrelationId to be a real value from the current context
        _context = new AggregateRootContext(
            CorrelationId.New(),
            this.GetPrimaryKeyString(),
            eventLog,
            this,
            true);

        var stateProviders = ServiceProvider.GetRequiredService<IAggregateRootStateProviders>();
        var stateProvider = await stateProviders.CreateFor<TState>(_context);
        _state = new AggregateRootState<TState>();
        _mutator = new StatefulAggregateRootMutator<TState>(_state, stateProvider);
        _mutation = new AggregateRootMutation(_context, _mutator, eventLog, ServiceProvider.GetRequiredService<ICausationManager>());

        await _mutator.Rehydrate();

        await OnActivate();
    }

    /// <inheritdoc/>
    public Task Apply<T>(T @event)
        where T : class
    {
        return _mutation?.Apply(@event) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<AggregateRootCommitResult> Commit()
    {
        return _mutation?.Commit() ?? Task.FromResult(AggregateRootCommitResult.Failed(ImmutableList<object>.Empty));
    }

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}
