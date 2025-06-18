// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/>.
/// </summary>
public class AggregateRoot : IAggregateRoot
{
    /// <summary>
    /// Context of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal IAggregateRootContext _context = default!;

    /// <summary>
    /// Mutation of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal IAggregateRootMutation _mutation = default!;

    /// <summary>
    /// Gets a value indicating whether the aggregate root is new.
    /// </summary>
    protected bool IsNew => !_context.HasEvents;

    public EventSequenceNumber TailEventSequenceNumber => _context.;

    /// <inheritdoc/>
    public async Task Apply(object @event) => await _mutation.Apply(@event);

    /// <inheritdoc/>
    public async Task<AggregateRootCommitResult> Commit()
    {
        var result = await _mutation.Commit();
        await _mutation.Mutator.Dehydrate();
        return result;
    }

    /// <summary>
    /// Chronicle Internal: Invoke the OnActivate method.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task InternalOnActivate() => OnActivate();

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}

/// <summary>
/// Represents a stateful implementation of <see cref="IAggregateRoot"/>.
/// </summary>
/// <typeparam name="TState">Type of state for the aggregate root.</typeparam>
public class AggregateRoot<TState> : AggregateRoot
{
    /// <summary>
    /// State of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal IAggregateRootState<TState> _state = default!;

    /// <summary>
    /// Gets the current state of the aggregate root.
    /// </summary>
    protected TState? State => _state.State;
}
