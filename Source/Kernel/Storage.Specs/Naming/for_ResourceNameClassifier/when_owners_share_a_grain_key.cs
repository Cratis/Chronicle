// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameClassifier;

public class when_owners_share_a_grain_key : given.a_classifier
{
    void Because() => _result = Classify(
    [
        Use(Namespace("a#b", "c"), "db:one", ResourceKind.NamespaceDatabase),
        Use(Namespace("a", "b#c"), "db:two", ResourceKind.NamespaceDatabase)
    ]);

    [Fact] void should_quarantine_the_first_namespace() => _result.QuarantinedNamespaces.ShouldContain(Namespace("a#b", "c"));
    [Fact] void should_quarantine_the_second_namespace() => _result.QuarantinedNamespaces.ShouldContain(Namespace("a", "b#c"));
}
