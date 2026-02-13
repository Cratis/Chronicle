// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Grains.StateMachines;

/// <summary>
/// Represents a base implementation of <see cref="IStateMachine{TStoredState}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of stored state.</typeparam>
public abstract class StateMachine<TStoredState> : Grain<TStoredState>, IStateMachine<TStoredState>
{
    static readonly NoOpState<TStoredState> _noOpState = new();

    Dictionary<Type, IState<TStoredState>> _states = [];
    IState<TStoredState> _currentState = _noOpState;
    bool _isTransitioning;
    bool _isLeaving;
    Type? _scheduledTransition;
    ILogger<StateMachine<StateMachineState>>? _logger;

    /// <summary>
    /// Gets whether or not the state machine is in a state.
    /// </summary>
    public bool IsInActiveState => _currentState.GetType() != _noOpState.GetType();

    /// <summary>
    /// Gets the initial state of the state machine.
    /// </summary>
    /// <returns>Type of initial state.</returns>
    protected virtual Type InitialState => typeof(NoOpState<TStoredState>);

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<StateMachine<StateMachineState>>>() ?? NullLogger<StateMachine<StateMachineState>>.Instance;

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

        var initialState = InitialState;

        if (State is StateMachineState stateMachineState && !string.IsNullOrEmpty(stateMachineState.CurrentState))
        {
            var state = _states.Values.FirstOrDefault(_ => _.GetType().FullName == stateMachineState.CurrentState) ?? throw new UnknownCurrentState(stateMachineState.CurrentState, GetType());
            initialState = state.GetType();
        }

        InvalidTypeForState.ThrowIfInvalid(initialState);
        ThrowIfUnknownStateType(initialState);
        await TransitionTo(initialState);
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        State = await _currentState.OnLeave(State);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IState<TStoredState>> GetCurrentState() => Task.FromResult(_currentState);

    /// <inheritdoc/>
    public Task<bool> CanTransitionTo<TState>()
        where TState : IState<TStoredState> => _currentState.CanTransitionTo<TState>(State);

    /// <inheritdoc/>
    public async Task TransitionTo<TState>()
        where TState : IState<TStoredState>
    {
        ThrowIfUnknownStateType(typeof(TState));
        if (await CanTransitionTo<TState>())
        {
            await TransitionTo(typeof(TState));
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
    /// Called when the state machine is activating.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for any cancellations.</param>
    /// <returns>Awaitable task.</returns>
    public virtual Task OnActivation(CancellationToken cancellationToken) => Task.CompletedTask;

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

    async Task TransitionTo(Type stateType)
    {
        _logger?.TransitioningTo(stateType);
        if (_isTransitioning)
        {
            if (_isLeaving)
            {
                throw new TransitioningDuringOnLeaveIsNotSupported(_currentState.GetType(), stateType, GetType());
            }
            _scheduledTransition = stateType;
            return;
        }

        _isTransitioning = true;
        _isLeaving = true;
        await OnBeforeLeavingState(_currentState);
        State = await _currentState.OnLeave(State);
        await OnAfterLeavingState(_currentState);
        _isLeaving = false;

        _currentState = _states[stateType];

        await OnBeforeEnteringState(_currentState);
        State = await _currentState.OnEnter(State);
        await OnAfterEnteringState(_currentState);

        if (State is StateMachineState stateMachineState)
        {
            stateMachineState.CurrentState = _currentState.GetType().FullName!;
        }

        await WriteStateAsync();

        _isTransitioning = false;
        if (_scheduledTransition != null)
        {
            var stateToTransitionTo = _scheduledTransition;
            _scheduledTransition = null;
            await TransitionTo(stateToTransitionTo);
        }
    }

    void ThrowIfUnknownStateType(Type type)
    {
        if (!_states.ContainsKey(type))
        {
            throw new UnknownStateTypeInStateMachine(type, GetType());
        }
    }
}
