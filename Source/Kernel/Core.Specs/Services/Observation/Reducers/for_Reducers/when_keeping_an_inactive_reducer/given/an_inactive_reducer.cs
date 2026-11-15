// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_keeping_an_inactive_reducer.given;

/// <summary>
/// An inactive reducer has no subscription to lose, only a registration a webhook may still hold back.
/// </summary>
public class an_inactive_reducer : Specification
{
    protected IReducer _reducer;
    protected ReducerDefinition _definition;
    protected bool _intact;

    void Establish()
    {
        _reducer = Substitute.For<IReducer>();
        _definition = new ReducerDefinition("the-reducer", EventSequenceId.Log, [], "the-read-model", false, []);
    }

    protected async Task Keep() => _intact = await Reducers.KeepInactive(_reducer, _definition);
}
