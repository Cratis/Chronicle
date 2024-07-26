// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutator"/> for stateful <see cref="IAggregateRoot"/>.
/// </summary>
/// <typeparam name="TState">Type of state.</typeparam>
/// <param name="state">The <see cref="IAggregateRootState{TState}"/> for the aggregate root.</param>
/// <param name="stateProvider">The <see cref="IAggregateRootStateProvider{TState}"/> for the aggregate root.</param>
public class StatefulAggregateRootMutator<TState>(
    IAggregateRootState<TState> state,
    IAggregateRootStateProvider<TState> stateProvider) : IAggregateRootMutator
    where TState : class
{
    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        if (state is IAggregateRootStateModifier<object> modifier)
        {
            modifier.SetState((await stateProvider.Provide())!);
        }
    }

    /// <inheritdoc/>
    public async Task Mutate(object @event)
    {
        if (state is IAggregateRootStateModifier<TState> modifier)
        {
            modifier.SetState((await stateProvider.Update(state.State, [@event]))!);
        }
    }

    /// <inheritdoc/>
    public async Task Dehydrate()
    {
        await stateProvider.Dehydrate();
    }
}
