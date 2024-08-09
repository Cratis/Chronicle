// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Orleans.Transactions;
using Cratis.Chronicle.Transactions;
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

    /// <inheritdoc/>
    public IAggregateRootContext? Context { get; set; }

    /// <summary>
    /// Gets a value indicating whether the aggregate root is new.
    /// </summary>
    protected bool IsNew => Context?.HasEventsForRehydration ?? true;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var eventStore = ServiceProvider.GetRequiredService<IEventStore>();
        var eventLog = ServiceProvider.GetRequiredService<IEventLog>();
        var eventSerializer = ServiceProvider.GetRequiredService<IEventSerializer>();
        var unitOfWorkManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var unitOfWork = unitOfWorkManager.Begin(CorrelationId.New());

        Context = new AggregateRootContext(
            this.GetPrimaryKeyString(),
            eventLog,
            this,
            unitOfWork);

        var eventHandlersFactory = ServiceProvider.GetRequiredService<IAggregateRootEventHandlersFactory>();
        var eventHandlers = eventHandlersFactory.GetFor(this);

        _mutator = new StatelessAggregateRootMutator(
            Context,
            eventStore,
            eventSerializer,
            eventHandlers);
        _mutation = new AggregateRootMutation(Context, _mutator, eventLog);

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

    /// <inheritdoc/>
    public IAggregateRootContext? Context { get; set; }

    /// <summary>
    /// Gets the current state of the aggregate root.
    /// </summary>
    public TState State => _state!.State;

    /// <summary>
    /// Gets a value indicating whether the aggregate root is new.
    /// </summary>
    protected bool IsNew => Context?.HasEventsForRehydration ?? true;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var correlationId = RequestContext.Get(Constants.CorrelationIdKey) as CorrelationId;
        correlationId ??= CorrelationId.New();
        var eventLog = ServiceProvider.GetRequiredService<IEventLog>();
        var unitOfWorkManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        if (!unitOfWorkManager.TryGetFor(correlationId, out var unitOfWork))
        {
            unitOfWork = unitOfWorkManager.Begin(CorrelationId.New());
        }

        Context = new AggregateRootContext(
            this.GetPrimaryKeyString(),
            eventLog,
            this,
            unitOfWork);

        var stateProviders = ServiceProvider.GetRequiredService<IAggregateRootStateProviders>();
        var stateProvider = await stateProviders.CreateFor<TState>(Context);
        _state = new AggregateRootState<TState>();
        _mutator = new StatefulAggregateRootMutator<TState>(_state, stateProvider);
        _mutation = new AggregateRootMutation(Context, _mutator, eventLog);

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
    public Task<AggregateRootCommitResult> Commit() => _mutation!.Commit();

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}
