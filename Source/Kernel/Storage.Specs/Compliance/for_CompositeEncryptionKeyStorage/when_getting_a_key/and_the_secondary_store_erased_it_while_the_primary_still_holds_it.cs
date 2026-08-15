// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

/// <summary>
/// The same divergence as its sibling, with the two stores the other way round - the primary answers first and the
/// fence is in a store the read never gets to.
/// </summary>
/// <remarks>
/// This is the ordering that a check of "the stores I read past" would miss entirely, and the reason every member
/// is asked instead. Which store kept a survivor and which one recorded the erasure is an accident of how the
/// composition was configured; a guarantee that turns on it is not a guarantee.
/// </remarks>
public class and_the_secondary_store_erased_it_while_the_primary_still_holds_it : given.two_key_stores
{
    EncryptionKey? _result;
    bool _stillInTheSecondary;

    async Task Establish()
    {
        var survivor = KeyNamed("survivor");
        await Save(_primary, survivor);
        await Save(_secondary, survivor);

        await _secondary.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _secondary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task Because()
    {
        _result = await KeyIn(_composite);
        _stillInTheSecondary = await HasKeyIn(_secondary);
    }

    [Fact] void should_not_serve_the_key_the_primary_still_holds() => _result.ShouldBeNull();
    [Fact] void should_leave_the_erased_store_without_it() => _stillInTheSecondary.ShouldBeFalse();
}
