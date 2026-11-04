// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_an_exemption_covers_only_some_pairs : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("$first$"), "db:shared"),
        Use(Store("$second$"), "db:shared"),
        Use(Store("third"), "db:shared")
    ],
    new([("$first$", []), ("$second$", [])]));

    [Fact] void should_quarantine_every_owner() => _result.QuarantinedStores.Count.ShouldEqual(3);
    [Fact] void should_report_the_overlap() => _result.Overlaps.Count.ShouldEqual(1);
}
