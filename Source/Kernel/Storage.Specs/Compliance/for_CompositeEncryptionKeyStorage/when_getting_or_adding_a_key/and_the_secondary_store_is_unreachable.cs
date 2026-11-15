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
        // An unreachable mirror may hold an erasure fence, so provisioning must not mint a key elsewhere.
        _error = await Catch.Exception(async () => _result = await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate));
        _inPrimary = await KeyIn(_primary);
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_return_a_key() => _result.ShouldBeNull();
    [Fact] void should_not_provision_it_on_the_primary_store() => _inPrimary.ShouldBeNull();
}
