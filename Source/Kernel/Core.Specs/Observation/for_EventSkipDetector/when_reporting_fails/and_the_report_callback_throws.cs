// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.when_reporting_fails;

public class and_the_report_callback_throws : given.a_detector
{
    EventSkipDetector _failing;
    Exception _exception;

    void Establish() => _failing = new(_ => false, (_, _, _, _) => throw new InvalidOperationException("report failed"));

    void Because()
    {
        _exception = Catch.Exception(() =>
        {
            _failing.HeldBack(_q, [(EventSequenceNumber)10UL], SuspectedSkipReason.HeldBackWhileCatchingUp);
            _failing.Evaluate(11);
            _failing.Evaluate(12);
        });
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
