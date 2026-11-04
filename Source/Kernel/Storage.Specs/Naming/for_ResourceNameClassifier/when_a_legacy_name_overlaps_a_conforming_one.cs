// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_a_legacy_name_overlaps_a_conforming_one : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("orders"), "mongo:host|ORDERS"),
        Use(Store("Orders+legacy"), "mongo:host|ORDERS")
    ]);

    [Fact] void should_quarantine_the_conforming_store() => _result.QuarantinedStores.ShouldContain("orders");
    [Fact] void should_quarantine_the_legacy_store() => _result.QuarantinedStores.ShouldContain("Orders+legacy");
}
