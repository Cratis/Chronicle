// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.when_asking;

public class and_a_rule_throws : given.rules
{
    ObserverJobAdmission _admission;
    Exception _error;

    void Establish()
    {
        var failing = Substitute.For<IObserverJobAdmissionRule>();
        failing.Order.Returns(100);
        failing.MayRun(Arg.Any<ObserverJobAdmissionContext>(), Arg.Any<CancellationToken>()).Returns<Task<bool>>(_ => throw new InvalidOperationException());
        _admission = new(_storage, [failing, Rule(200, true)]);
    }

    async Task Because() => _error = await Catch.Exception(() => _admission.MayRun(AdmissionFor(_request), CancellationToken.None));

    [Fact] void should_let_the_failure_propagate() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_ask_the_rules_after_it() => _asked.ShouldBeEmpty();
}
