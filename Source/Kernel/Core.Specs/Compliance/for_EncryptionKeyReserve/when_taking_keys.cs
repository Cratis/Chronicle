// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyReserve;

public class when_taking_keys : Specification
{
    const int Capacity = 4;
    const int Taken = 50;

    ConcurrentBag<EncryptionKey> _generated;
    EncryptionKeyReserve _reserve;
    List<EncryptionKey> _taken;

    void Establish()
    {
        _generated = [];
        var counter = 0;
        _reserve = new(
            () =>
            {
                var number = BitConverter.GetBytes(Interlocked.Increment(ref counter));
                var key = new EncryptionKey(number, number);
                _generated.Add(key);
                return key;
            },
            Capacity);
    }

    async Task Because()
    {
        _taken = [];
        for (var index = 0; index < Taken; index++)
        {
            _taken.Add(_reserve.Take());
        }

        // Let the background refill settle so the reserve's own bound can be observed.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_generated.Count < Taken + Capacity && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }

    [Fact] void should_hand_every_key_out_once() => _taken.Distinct().Count().ShouldEqual(Taken);
    [Fact] void should_only_hand_out_generated_keys() => _taken.TrueForAll(_generated.Contains).ShouldBeTrue();
    [Fact] void should_not_generate_more_than_it_handed_out_and_keeps_in_reserve() => (_generated.Count <= Taken + Capacity).ShouldBeTrue();
}
