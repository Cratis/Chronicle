// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_two_namespaces_share_a_read_model_database : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("shop"), "db:shop-es"),
        Use(Namespace("shop", "one"), "db:shared", ResourceKind.ReadModelDatabase),
        Use(Namespace("shop", "two"), "db:shared", ResourceKind.ReadModelDatabase)
    ]);

    [Fact] void should_quarantine_both_namespaces() => _result.QuarantinedNamespaces.Count.ShouldEqual(2);
    [Fact] void should_quarantine_the_first_namespace() => _result.QuarantinedNamespaces.ShouldContain(Namespace("shop", "one"));
    [Fact] void should_quarantine_the_second_namespace() => _result.QuarantinedNamespaces.ShouldContain(Namespace("shop", "two"));
    [Fact] void should_not_quarantine_the_store() => _result.QuarantinedStores.ShouldBeEmpty();
}
