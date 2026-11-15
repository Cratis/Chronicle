// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceCasingMerge;

public class when_two_casings_and_none_has_data : Specification
{
    IList<NamespaceState> _kept;

    void Because() => (_kept, _) = NamespaceCasingMerge.Merge(
        [new("cratis", DateTimeOffset.MinValue), new("Cratis", DateTimeOffset.MinValue)], new HashSet<Concepts.EventStoreNamespaceName>());

    [Fact] void should_keep_the_first_stored_name_on_a_tie() => _kept.Select(_ => _.Name.Value).ShouldContainOnly("cratis");
}
