// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

/// <summary>
/// Golden-value specs pinning the exact content-hash output for representative events. The hash is used to
/// verify previously stored events' content, so a changed hash silently breaks every stored ContentHash.
/// These values were captured from the original implementation and must remain byte-for-byte identical.
/// </summary>
public class when_calculating_hash_for_representative_events : given.representative_events
{
    string _emptyHash;
    string _scalarsHash;
    string _unicodeHash;
    string _nestedHash;
    string _arraysHash;
    string _unsortedKeysHash;

    void Because()
    {
        _emptyHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _empty);
        _scalarsHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _scalars);
        _unicodeHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _unicode);
        _nestedHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _nested);
        _arraysHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _arrays);
        _unsortedKeysHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _unsortedKeys);
    }

    [Fact] void should_match_golden_for_empty_content() => _emptyHash.ShouldEqual("Xt/WvAkQEIOex8k9F8n0dSxZOKpL6Sz5m6Vzim2tHyo=");

    [Fact] void should_match_golden_for_all_scalar_types() => _scalarsHash.ShouldEqual("dtLMQLuKiQDTuYGBsvszYBOqAbD296KAKFRRaFaFjSQ=");

    [Fact] void should_match_golden_for_unicode_content() => _unicodeHash.ShouldEqual("FbS6mzc4fVrOq6WRIfDBprtpnxMMwyiZzm3yarjPz+Q=");

    [Fact] void should_match_golden_for_nested_objects() => _nestedHash.ShouldEqual("JL6IQGhxi6G7CjVh/67ZnGDVRasAmUBYt3t/pHBatzU=");

    [Fact] void should_match_golden_for_arrays() => _arraysHash.ShouldEqual("Sfv9UsNRwwC5j0vK3ggUk7d5SrqK/pqqlEeAgVkODPU=");

    [Fact] void should_match_golden_for_unsorted_keys() => _unsortedKeysHash.ShouldEqual("rHOIBN28B9DSuXCHaXotl60N/ZFcUZQGucM20BvD7Ck=");
}
