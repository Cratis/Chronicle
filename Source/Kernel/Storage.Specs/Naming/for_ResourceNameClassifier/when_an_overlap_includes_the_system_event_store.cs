// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_an_overlap_includes_the_system_event_store : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("System"), "mongo:host|_SYSTEM+ES"),
        Use(Store("other"), "mongo:host|_SYSTEM+ES")
    ]);

    [Fact] void should_quarantine_only_the_other_owner() => _result.QuarantinedStores.ShouldContainOnly("other");
    [Fact] void should_report_the_overlap() => _result.Overlaps.Count.ShouldEqual(1);
}
