// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_DigestKeyedEventSequenceMutationRegistry;

public class when_the_key_is_available : Specification
{
    static readonly byte[] _canonical = [1, 2, 3];

    EventSequenceMutationDigestKey _key;
    IEventSequenceMutationRegistry _inner;
    DigestKeyedEventSequenceMutationRegistry _registry;
    byte[] _computedByTheProvider;
    EventSequenceMutationBeginResult _expected;
    EventSequenceMutationBeginResult _begin;

    void Establish()
    {
        // A key other than the spec key of the event store, so the spec proves the scoped key is the one used.
        _key = EventSequenceMutationDigestKey.DeriveFrom("event-store", new byte[32]);
        var keys = Substitute.For<IEventSequenceMutationDigestKeys>();
        keys.TryGetFor("event-store").Returns(_key);
        _expected = EventSequenceMutationBeginResult.Contended();
        _inner = Substitute.For<IEventSequenceMutationRegistry>();
        _inner.Begin(Arg.Any<EventSequenceMutationRequest>(), Arg.Any<EventSequenceMutationTarget>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Yield();
                _computedByTheProvider = EventSequenceMutationDigestSeam.Compute("event-store", EventSequenceMutationDigestVersion.HmacSha256PerEventStore, _canonical);
                return _expected;
            });
        _registry = new("event-store", keys, _inner);
    }

    async Task Because() => _begin = await _registry.Begin(null!, null!);

    [Fact] void should_return_the_provider_result() => ReferenceEquals(_begin, _expected).ShouldBeTrue();
    [Fact] void should_compute_with_the_event_stores_key() => _computedByTheProvider.SequenceEqual(_key.Compute(_canonical)).ShouldBeTrue();
}
