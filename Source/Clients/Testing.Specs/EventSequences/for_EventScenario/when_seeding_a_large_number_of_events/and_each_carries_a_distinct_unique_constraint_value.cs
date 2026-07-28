// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_seeding_a_large_number_of_events;

public class and_each_carries_a_distinct_unique_constraint_value : Specification, IDisposable
{
    const int NumberOfLicenses = 1000;

    EventScenario _scenario;
    LicenseKey[] _keys;
    AppendResult _reissueOfTheFirstKey;

    void Establish()
    {
        _scenario = new EventScenario();
        _keys = [.. Enumerable.Range(0, NumberOfLicenses).Select(_ => new LicenseKey(Guid.NewGuid()))];
    }

    async Task Because()
    {
        foreach (var key in _keys)
        {
            await _scenario.Given
                .ForEventSource(EventSourceId.New())
                .Events(new LicenseIssued(key));
        }

        _reissueOfTheFirstKey = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new LicenseIssued(_keys[0]));
    }

    [Fact] async Task should_have_appended_every_seeded_license() => (await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(NumberOfLicenses);
    [Fact] void should_still_detect_a_duplicate_against_the_full_set() => _reissueOfTheFirstKey.ShouldHaveConstraintViolationFor(UniqueLicenseKey.Name);

    public void Dispose() => _scenario.Dispose();
}
