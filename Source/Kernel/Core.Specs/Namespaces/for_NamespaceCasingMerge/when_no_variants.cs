// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingMerge;

public class when_no_variants : Specification
{
    IList<NamespaceState> _kept;
    IList<(EventStoreNamespaceName Variant, EventStoreNamespaceName Canonical)> _merged;
    readonly NamespaceState[] _stored = [new("second", DateTimeOffset.MaxValue), new("first", DateTimeOffset.MinValue)];

    void Because() => (_kept, _merged) = NamespaceCasingMerge.Merge(_stored, new HashSet<EventStoreNamespaceName>());

    [Fact] void should_preserve_storage_order() => _kept.SequenceEqual(_stored).ShouldBeTrue();
    [Fact] void should_not_merge_any_names() => _merged.ShouldBeEmpty();
}
