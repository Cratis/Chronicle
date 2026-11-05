// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKeys;

public class when_the_key_store_fails : given.a_key_store
{
    IEncryptionKeyStorage _failing;
    EventSequenceMutationDigestKey? _whileFailing;
    EventSequenceMutationDigestKey? _afterRecovery;

    void Establish()
    {
        _failing = Substitute.For<IEncryptionKeyStorage>();
        _failing.GetOrAddInternalSecretFor(Arg.Any<EventStoreName>(), Arg.Any<InternalSecretPurpose>(), Arg.Any<EncryptionKey>())
            .Returns(
                _ => Task.FromException<EncryptionKey>(new InvalidOperationException("unreachable")),
                call => _keyStore.GetOrAddInternalSecretFor(call.ArgAt<EventStoreName>(0), call.ArgAt<InternalSecretPurpose>(1), call.ArgAt<EncryptionKey>(2)));
    }

    async Task Because()
    {
        var keys = KeysOver(_failing);
        _whileFailing = await keys.TryGetFor("event-store");
        _afterRecovery = await keys.TryGetFor("event-store");
    }

    [Fact] void should_have_no_key_while_failing() => _whileFailing.ShouldBeNull();
    [Fact] void should_not_cache_the_failure() => _afterRecovery.ShouldNotBeNull();
}
