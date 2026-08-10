// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_the_secondary_store_is_unreachable : given.two_key_stores
{
    EncryptionKey _candidate;
    EncryptionKey _result;
    EncryptionKey? _inPrimary;
    Exception _error;

    void Establish()
    {
        _candidate = KeyNamed("candidate");
        _composite = new CompositeEncryptionKeyStorage(_primary, AnUnreachableStore());
    }

    async Task Because()
    {
        // A protected value must still be writable while a store being mirrored to is down - the read path
        // heals that store the next time the key is asked for.
        _error = await Catch.Exception(async () => _result = await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate));
        _inPrimary = await KeyIn(_primary);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_return_the_provisioned_key() => _result.ShouldEqual(_candidate);
    [Fact] void should_provision_it_on_the_primary_store() => _inPrimary.ShouldEqual(_candidate);
}
