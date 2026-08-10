// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_deleting_a_key;

public class and_a_specific_revision_is_given : given.two_key_stores
{
    static readonly EncryptionKeyRevision _second = new(2u);

    bool _primaryHasInitial;
    bool _secondaryHasInitial;
    bool _primaryHasSecond;
    bool _secondaryHasSecond;

    async Task Establish()
    {
        await Save(_primary, KeyNamed("initial"), EncryptionKeyRevision.Initial);
        await Save(_primary, KeyNamed("second"), _second);
        await Save(_secondary, KeyNamed("initial"), EncryptionKeyRevision.Initial);
        await Save(_secondary, KeyNamed("second"), _second);
    }

    async Task Because()
    {
        await _composite.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _second);
        _primaryHasInitial = await HasKeyIn(_primary, EncryptionKeyRevision.Initial);
        _secondaryHasInitial = await HasKeyIn(_secondary, EncryptionKeyRevision.Initial);
        _primaryHasSecond = await HasKeyIn(_primary, _second);
        _secondaryHasSecond = await HasKeyIn(_secondary, _second);
    }

    [Fact] void should_erase_the_revision_from_the_primary_store() => _primaryHasSecond.ShouldBeFalse();
    [Fact] void should_erase_the_revision_from_the_secondary_store() => _secondaryHasSecond.ShouldBeFalse();
    [Fact] void should_keep_the_other_revision_in_the_primary_store() => _primaryHasInitial.ShouldBeTrue();
    [Fact] void should_keep_the_other_revision_in_the_secondary_store() => _secondaryHasInitial.ShouldBeTrue();
}
