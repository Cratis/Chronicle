// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_a_name_breaks_the_rule_without_overlapping : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Store("a+es+b"), "mongo:host|A+ES+B"),
        Use(Namespace("a+es+b", "x#y"), "mongo:host|A+ES+B+ES+X#Y", ResourceKind.NamespaceDatabase),
        Use(Store("orders"), "mongo:host|ORDERS")
    ]);

    [Fact] void should_not_quarantine_any_store() => _result.QuarantinedStores.ShouldBeEmpty();
    [Fact] void should_not_quarantine_any_namespace() => _result.QuarantinedNamespaces.ShouldBeEmpty();
}
