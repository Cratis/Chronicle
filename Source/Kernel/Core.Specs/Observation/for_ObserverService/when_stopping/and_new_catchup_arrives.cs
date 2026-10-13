// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_stopping;

public class and_new_catchup_arrives : Specification
{
    readonly ObserverServiceDrain _drain = new();
    Exception _error;
    bool _handlerInvoked;

    async Task Establish() => await _drain.Stop((_, _, _) => Task.CompletedTask, CancellationToken.None);

    async Task Because() => _error = await Catch.Exception(() => _drain.Run(new(ObserverKey.NotSet, ObserverType.Projection), false, () =>
    {
        _handlerInvoked = true;
        return Task.FromResult(true);
    }));

    [Fact] void should_refuse_the_work() => _error.ShouldBeOfExactType<ObserverServiceStopping>();
    [Fact] void should_not_invoke_the_handler() => _handlerInvoked.ShouldBeFalse();
}
