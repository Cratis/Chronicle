// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ObserverDeliveries.when_finding_unknown_overrides;

public class and_an_override_matches_no_observer : Specification
{
    IReadOnlyList<string> _result;

    void Because()
    {
        var options = new ChronicleOptions { Serverless = { Observers = { ["known"] = new(), ["typo"] = new() } } };
        _result = new ObserverDeliveries(options).UnknownOverrides(["known"]);
    }

    [Fact] void should_report_only_the_unmatched_key() => _result.ShouldEqual("typo");
}
