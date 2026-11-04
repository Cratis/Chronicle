// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_names_differ_only_by_normalization_and_the_provider_treats_them_as_distinct : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("caf\u00e9"), "mongo:host|CAF\u00c9"),
        Use(Store("cafe\u0301"), "mongo:host|CAFE\u0301")
    ]);

    [Fact] void should_not_quarantine_any_store() => _result.QuarantinedStores.ShouldBeEmpty();
    [Fact] void should_not_report_an_overlap() => _result.Overlaps.ShouldBeEmpty();
}
