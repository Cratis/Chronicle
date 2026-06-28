// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that when a reducer raises an error, <see cref="ReadModelScenario{TReadModel}"/> surfaces the
/// failure rather than returning the partial state as a successful projection.
/// </summary>
public class when_a_reducer_throws : Specification
{
    ReadModelScenario<Tally> _scenario;
    EventSourceId _id;
    Exception _error;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<Tally>();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_id).Events(new TallyBroke());
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_surface_the_reducer_failure() => _error.ShouldBeOfExactType<ReducerFailed>();
}
