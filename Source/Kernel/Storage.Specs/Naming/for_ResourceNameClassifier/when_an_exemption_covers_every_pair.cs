// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_an_exemption_covers_every_pair : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("$first$"), "db:shared"),
        Use(Store("$second$"), "db:shared")
    ],
    new([("$first$", []), ("$second$", [])]));

    [Fact] void should_not_quarantine_any_store() => _result.QuarantinedStores.ShouldBeEmpty();
    [Fact] void should_not_report_an_overlap() => _result.Overlaps.ShouldBeEmpty();
}
