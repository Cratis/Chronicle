// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingMerge;

public class when_a_default_variant_is_stored : Specification
{
    IList<NamespaceState> _kept;
    IList<(EventStoreNamespaceName Variant, EventStoreNamespaceName Canonical)> _merged;

    void Because() => (_kept, _merged) = NamespaceCasingMerge.Merge(
        [new("default", DateTimeOffset.MinValue)], new HashSet<EventStoreNamespaceName> { "default" });

    [Fact] void should_pin_default_to_its_reserved_casing() => _kept.Single().Name.ShouldEqual(EventStoreNamespaceName.Default);
    [Fact] void should_report_the_variant() => _merged.ShouldContainOnly(((EventStoreNamespaceName)"default", EventStoreNamespaceName.Default));
}
