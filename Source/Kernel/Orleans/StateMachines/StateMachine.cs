// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Represents a base implementation of <see cref="IStateMachine{TStoredState}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of stored state.</typeparam>
public abstract class StateMachine<TStoredState> : Grain<TStoredState>, IStateMachine<TStoredState>
{
    static readonly NoOpState<TStoredState> _noOpState = new();

    IDictionary<Type, IState<TStoredState>> _states = new Dictionary<Type, IState<TStoredState>>();
    IState<TStoredState> _currentState = _noOpState;

    /// <summary>
    /// Gets the initial state of the state machine.
    /// </summary>
    /// <returns>Type of initial state.</returns>
    protected virtual Type InitialState => typeof(NoOpState<TStoredState>);

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await OnActivation(cancellationToken);
        _states = GetStates().ToDictionary(_ => _.GetType());
        _states[typeof(NoOpState<TStoredState>)] = new NoOpState<TStoredState>();
        _currentState = _noOpState;
    }

    /// <inheritdoc/>
    public Task<bool> CanTransitionTo<TState>()
        where TState : IState<TStoredState> => _states[typeof(TState)].CanTransitionTo<TState>(State);

    /// <inheritdoc/>
    public async Task TransitionTo<TState>()
        where TState : IState<TStoredState>
    {
        if (await CanTransitionTo<TState>())
        {
            await _currentState.OnLeave(State);
            _currentState = _states[typeof(TState)];
            await _currentState.OnEnter(State);
        }
    }

    /// <summary>
    /// Gets the states for this state machine.
    /// </summary>
    /// <returns>A collection of states.</returns>
    public abstract IImmutableList<IState<TStoredState>> GetStates();

    /// <summary>
    /// Called when the state machine is activated.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for any cancellations.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivation(CancellationToken cancellationToken) => Task.CompletedTask;
}
