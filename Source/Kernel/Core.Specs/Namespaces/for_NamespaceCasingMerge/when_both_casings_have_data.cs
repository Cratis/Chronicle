// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingMerge;

public class when_both_casings_have_data : Specification
{
    IList<NamespaceState> _kept;

    void Because() => (_kept, _) = NamespaceCasingMerge.Merge(
        [new("cratis", DateTimeOffset.MaxValue), new("Cratis", DateTimeOffset.MinValue)],
        new HashSet<EventStoreNamespaceName> { "Cratis", "cratis" });

    [Fact] void should_keep_the_earliest_created_name() => _kept.Select(_ => _.Name.Value).ShouldContainOnly("Cratis");
}
