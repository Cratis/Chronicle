// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_asserting;

public class without_the_expected_effect : given.a_scenario
{
    Exception? _receiptFailure;
    Exception? _quarantineFailure;
    Exception? _eventFailure;

    void Because()
    {
        _receiptFailure = Catch.Exception(() => _scenario.ShouldHaveReceipt(new(_recorded.Id, "absent", new(_recorded.Id, 1))));
        _quarantineFailure = Catch.Exception(() => _scenario.ShouldBeQuarantined(_recorded.Id));
        _eventFailure = Catch.Exception(_scenario.ShouldHaveAppended<EffectWasRecorded>);
    }

    [Fact] void should_fail_the_receipt_assertion_synchronously() => _receiptFailure.ShouldBeOfExactType<OccurrenceScenarioAssertionFailed>();
    [Fact] void should_fail_the_quarantine_assertion_synchronously() => _quarantineFailure.ShouldBeOfExactType<OccurrenceScenarioAssertionFailed>();
    [Fact] void should_fail_the_event_assertion_synchronously() => _eventFailure.ShouldBeOfExactType<OccurrenceScenarioAssertionFailed>();
}
