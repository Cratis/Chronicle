// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_fast_forwarding;

public class and_the_observer_is_retired : given.a_retired_observer
{
    FastForwardResult _result;

    async Task Because() => _result = await _observer.FastForward();

    [Fact] void should_report_not_available() => _result.Outcome.ShouldEqual(FastForwardOutcome.NotAvailable);
}
