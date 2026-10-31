// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_ScenarioTime.when_advancing;

public class to_an_earlier_instant : Specification
{
    ScenarioTime _time;
    Exception? _error;

    void Establish() => _time = new();
    void Because() => _error = Catch.Exception(() => _time.AdvanceTo(_time.Now.AddTicks(-1)));

    [Fact] void should_reject_moving_backwards() => _error.ShouldBeOfExactType<ScenarioTimeCannotMoveBackwards>();
    [Fact] void should_preserve_the_instant() => _time.Now.ShouldEqual(new ScenarioTime().Now);
}
