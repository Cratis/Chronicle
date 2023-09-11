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
        _states = CreateStates().ToDictionary(_ => _.GetType());
        _states[typeof(NoOpState<TStoredState>)] = new NoOpState<TStoredState>();
        foreach (var state in _states.Values)
        {
            if (state is State<TStoredState> actualState)
            {
                actualState._stateMachine = this;
            }
        }

        InvalidTypeForState.ThrowIfInvalid(InitialState);
        ThrowIfUnknownStateType(InitialState);
        _currentState = _states[InitialState];
        await _currentState.OnEnter(State);
    }

    /// <inheritdoc/>
    public Task<IState<TStoredState>> GetCurrentState() => Task.FromResult(_currentState);

    /// <inheritdoc/>
    public Task<bool> CanTransitionTo<TState>()
        where TState : IState<TStoredState> => _states[typeof(TState)].CanTransitionTo<TState>(State);

    /// <inheritdoc/>
    public async Task TransitionTo<TState>()
        where TState : IState<TStoredState>
    {
        ThrowIfUnknownStateType(typeof(TState));
        if (await CanTransitionTo<TState>())
        {
            await OnBeforeLeavingState(_currentState);
            State = await _currentState.OnLeave(State);
            await OnAfterLeavingState(_currentState);

            _currentState = _states[typeof(TState)];

            await OnBeforeEnteringState(_currentState);
            State = await _currentState.OnEnter(State);
            await OnAfterEnteringState(_currentState);

            await WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public Task<IImmutableList<IState<TStoredState>>> GetStates() => Task.FromResult<IImmutableList<IState<TStoredState>>>(_states.Values.ToImmutableList());

    /// <summary>
    /// Gets the states for this state machine.
    /// </summary>
    /// <returns>A collection of states.</returns>
    public abstract IImmutableList<IState<TStoredState>> CreateStates();

    /// <summary>
    /// Called when the state machine is activated.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for any cancellations.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivation(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called before entering a state.
    /// </summary>
    /// <param name="state">Instance of the state that will be entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnBeforeEnteringState(IState<TStoredState> state) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called after entering a state.
    /// </summary>
    /// <param name="state">Instance of the state that was entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnAfterEnteringState(IState<TStoredState> state) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called before leaving a state.
    /// </summary>
    /// <param name="state">Instance of the state that will be entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnBeforeLeavingState(IState<TStoredState> state) => Task.CompletedTask;

    /// <summary>
    /// Method that gets called after leaving a state.
    /// </summary>
    /// <param name="state">Instance of the state that was entered.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnAfterLeavingState(IState<TStoredState> state) => Task.CompletedTask;

    void ThrowIfUnknownStateType(Type type)
    {
        if (!_states.ContainsKey(type))
        {
            throw new UnknownStateTypeInStateMachine(type, GetType());
        }
    }
}
