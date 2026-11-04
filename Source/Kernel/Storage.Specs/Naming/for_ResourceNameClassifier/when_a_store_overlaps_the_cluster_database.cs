// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_a_store_overlaps_the_cluster_database : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(null, "mongo:host|CHRONICLE", ResourceKind.Kernel),
        Use(Store("chronicle"), "mongo:host|CHRONICLE", ResourceKind.ReadModelDatabase)
    ]);

    [Fact] void should_quarantine_the_store() => _result.QuarantinedStores.ShouldContainOnly("chronicle");
    [Fact] void should_report_the_overlap_with_the_kernel() => _result.Overlaps.Single().Owners.Any(_ => _ is null).ShouldBeTrue();
}
