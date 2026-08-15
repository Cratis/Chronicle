// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_a_key;

/// <summary>
/// A composite provisions on its primary only, so a fence that exists in a later store would be invisible to
/// provisioning unless the composite asks. Without that, adding a store to the composition - or an erasure that
/// reached the secondary first - would make the primary the one place the subject gets a key again.
/// </summary>
public class and_one_store_has_erased_the_subject : given.two_key_stores
{
    Exception _error;
    EncryptionKey? _inPrimaryAfterwards;

    async Task Establish() =>
        await _secondary.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    async Task Because()
    {
        _error = await Catch.Exception(async () => await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("candidate")));
        _inPrimaryAfterwards = await KeyIn(_primary);
    }

    [Fact] void should_refuse_to_provision() => _error.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_not_provision_on_the_primary_store() => _inPrimaryAfterwards.ShouldBeNull();
}
