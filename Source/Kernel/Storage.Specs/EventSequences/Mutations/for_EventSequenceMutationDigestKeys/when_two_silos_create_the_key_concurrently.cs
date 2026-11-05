// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKeys;

public class when_two_silos_create_the_key_concurrently : given.a_key_store
{
    EventSequenceMutationDigestKey? _first;
    EventSequenceMutationDigestKey? _second;
    List<EncryptionKey> _candidates;
    EncryptionKey _stored;

    async Task Because()
    {
        // Both silos are held at the key store until both have generated their own candidate, so the store decides.
        _candidates = [];
        var bothArrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var racing = Substitute.For<IEncryptionKeyStorage>();
        racing.GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>())
            .Returns(async call =>
            {
                lock (_candidates)
                {
                    _candidates.Add(call.ArgAt<EncryptionKey>(3));
                    if (_candidates.Count == 2)
                    {
                        bothArrived.SetResult();
                    }
                }

                await bothArrived.Task;
                return await _keyStore.GetOrAddFor(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2), call.ArgAt<EncryptionKey>(3));
            });

        var first = Task.Run(() => KeysOver(racing).TryGetFor("event-store"));
        var second = Task.Run(() => KeysOver(racing).TryGetFor("event-store"));
        _first = await first;
        _second = await second;
        _stored = await _keyStore.GetFor("event-store", EventSequenceMutationDigestKeyIdentifiers.Namespace, EventSequenceMutationDigestKeyIdentifiers.Secret);
    }

    [Fact] void should_have_proposed_two_different_secrets() => _candidates[0].Private.SequenceEqual(_candidates[1].Private).ShouldBeFalse();
    [Fact] void should_converge_on_one_key() => _first!.Compute(_canonical).SequenceEqual(_second!.Compute(_canonical)).ShouldBeTrue();

    [Fact] void should_use_the_stored_secret() =>
        _first!.Compute(_canonical).SequenceEqual(EventSequenceMutationDigestKey.DeriveFrom("event-store", _stored.Private).Compute(_canonical)).ShouldBeTrue();
}
