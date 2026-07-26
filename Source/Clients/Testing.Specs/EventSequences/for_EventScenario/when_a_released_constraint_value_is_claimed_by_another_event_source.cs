// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// An event source holds at most one value per unique constraint, so reissuing under a new key releases the old one.
/// A second event source must then be able to claim it - the harness has to agree with the kernel's storage here or
/// specs reject an append the real event store would accept.
/// </summary>
public class when_a_released_constraint_value_is_claimed_by_another_event_source : Specification, IDisposable
{
    static readonly LicenseKey _releasedKey = new(Guid.NewGuid());
    static readonly LicenseKey _currentKey = new(Guid.NewGuid());
    static readonly EventSourceId _holder = EventSourceId.New();

    EventScenario _scenario;
    AppendResult _claimOfReleasedKey;
    AppendResult _claimOfCurrentKey;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_holder)
            .Events(new LicenseIssued(_releasedKey));

        await _scenario.Given
            .ForEventSource(_holder)
            .Events(new LicenseReissued(_currentKey));

        _claimOfReleasedKey = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new LicenseIssued(_releasedKey));

        _claimOfCurrentKey = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new LicenseIssued(_currentKey));
    }

    [Fact] void should_accept_the_claim_of_the_released_key() => _claimOfReleasedKey.ShouldBeSuccessful();
    [Fact] void should_reject_the_claim_of_the_key_still_held() => _claimOfCurrentKey.ShouldHaveConstraintViolation(UniqueLicenseKey.Name);

    public void Dispose() => _scenario.Dispose();
}
