// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_two_stores_share_an_event_store_database : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("first"), "mongo:host|SHARED"),
        Use(Store("second"), "mongo:host|SHARED"),
        Use(Store("third"), "mongo:host|THIRD")
    ]);

    [Fact] void should_quarantine_both_stores() => _result.QuarantinedStores.ShouldContainOnly("first", "second");
    [Fact] void should_not_quarantine_other_stores() => _result.QuarantinedStores.Contains("third").ShouldBeFalse();
    [Fact] void should_not_quarantine_any_namespace() => _result.QuarantinedNamespaces.ShouldBeEmpty();
    [Fact] void should_report_one_overlap() => _result.Overlaps.Count.ShouldEqual(1);
}
