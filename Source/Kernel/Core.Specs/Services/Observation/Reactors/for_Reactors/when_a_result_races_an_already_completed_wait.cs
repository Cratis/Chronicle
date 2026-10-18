// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_a_result_races_an_already_completed_wait : given.an_observation_stream
{
    Exception _exception;

    void Establish()
    {
        var completion = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _deliver("partition", [], completion);
        completion.TrySetResult(ObserverSubscriberResult.Disconnected());
    }

    void Because() => _exception = Catch.Exception(() => Reply());

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
