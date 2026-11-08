// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestSeam;

public class when_computing_a_keyed_digest : Specification
{
    const EventSequenceMutationDigestVersion Keyed = EventSequenceMutationDigestVersion.HmacSha256PerEventStore;
    static readonly byte[] _canonical = [1, 2, 3];

    byte[] _first;
    byte[] _second;
    byte[] _firstAgain;
    byte[] _unkeyed;
    Exception _withoutKey;
    Exception _withAnotherStoresKey;

    void Because()
    {
        _first = EventSequenceMutationDigestSeam.Compute("first", Keyed, _canonical);
        _second = EventSequenceMutationDigestSeam.Compute("second", Keyed, _canonical);
        _unkeyed = EventSequenceMutationDigestSeam.Compute("first", EventSequenceMutationDigestVersion.Sha256DomainSeparated, _canonical);

        using (EventSequenceMutationDigestSeam.UseKey(EventSequenceMutationDigestTestKeys.For("first")))
        {
            _firstAgain = EventSequenceMutationDigestSeam.Compute("first", Keyed, _canonical);
            _withAnotherStoresKey = Catch.Exception(() => EventSequenceMutationDigestSeam.Compute("second", Keyed, _canonical));
        }

        // An explicit scope without a key hides the spec keys, as production has none.
        using (EventSequenceMutationDigestSeam.UseKey(null))
        {
            _withoutKey = Catch.Exception(() => EventSequenceMutationDigestSeam.Compute((EventStoreName)"first", Keyed, _canonical));
        }
    }

    [Fact] void should_differ_between_event_stores() => _first.SequenceEqual(_second).ShouldBeFalse();
    [Fact] void should_differ_from_the_unkeyed_digest() => _first.SequenceEqual(_unkeyed).ShouldBeFalse();
    [Fact] void should_be_deterministic_for_the_same_key() => _firstAgain.SequenceEqual(_first).ShouldBeTrue();
    [Fact] void should_fail_closed_without_a_key() => _withoutKey.ShouldBeOfExactType<EventSequenceMutationDigestKeyUnavailable>();
    [Fact] void should_fail_closed_with_another_event_stores_key() => _withAnotherStoresKey.ShouldBeOfExactType<EventSequenceMutationDigestKeyUnavailable>();
}
