// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_two_collections_changed : a_collapse_specification
{
    object[] _changedContacts;
    object[] _changedAddresses;
    IReadOnlyList<PropertyDifference> _result;

    void Establish()
    {
        _changedContacts = Collection(Expando(("name", "cipher")));
        _changedAddresses = Collection(Expando(("city", "cipher")));
    }

    void Because()
    {
        var changed = Expando(("contacts", _changedContacts), ("addresses", _changedAddresses));
        _result = new[]
        {
            new PropertyDifference("[contacts].name", "Jane", "cipher"),
            new PropertyDifference("[addresses].city", "Oslo", "cipher")
        }.Collapse(changed, changed);
    }

    [Fact] void should_collapse_each_collection_independently() => _result.Count.ShouldEqual(2);

    [Fact] void should_replace_the_whole_contacts_collection() => _result.Count(_ => ReferenceEquals(_.Changed, _changedContacts)).ShouldEqual(1);

    [Fact] void should_replace_the_whole_addresses_collection() => _result.Count(_ => ReferenceEquals(_.Changed, _changedAddresses)).ShouldEqual(1);
}
