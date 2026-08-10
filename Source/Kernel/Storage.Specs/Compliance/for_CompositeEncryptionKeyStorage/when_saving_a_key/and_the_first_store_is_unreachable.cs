// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_saving_a_key;

public class and_the_first_store_is_unreachable : given.two_key_stores
{
    EncryptionKey _key;
    Exception _error;
    EncryptionKey? _inSecondary;

    void Establish()
    {
        _key = KeyNamed("saved");
        _composite = new CompositeEncryptionKeyStorage(AnUnreachableStore(), _secondary);
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () => await _composite.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _key));
        _inSecondary = await KeyIn(_secondary);
    }

    [Fact] void should_still_save_it_in_every_reachable_store() => _inSecondary.ShouldEqual(_key);
    [Fact] void should_report_the_save_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeySaveIncomplete>();
}
