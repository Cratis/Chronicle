// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingMerge;

public class when_two_casings_and_one_has_data : Specification
{
    IList<NamespaceState> _kept;
    IList<(EventStoreNamespaceName Variant, EventStoreNamespaceName Canonical)> _merged;

    void Because() => (_kept, _merged) = NamespaceCasingMerge.Merge(
        [new("cratis", DateTimeOffset.MinValue), new("Cratis", DateTimeOffset.MaxValue)],
        new HashSet<EventStoreNamespaceName> { "Cratis" });

    [Fact] void should_keep_the_name_with_its_own_data() => _kept.Select(_ => _.Name.Value).ShouldContainOnly("Cratis");
    [Fact] void should_merge_the_variant_into_the_name_with_data() => _merged.ShouldContainOnly(((EventStoreNamespaceName)"cratis", (EventStoreNamespaceName)"Cratis"));
}
