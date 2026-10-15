// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.when_checking_if_allowed;

public class and_mode_is_per_value_and_an_earlier_value_is_claimed : given.a_unique_constraints_storage
{
    (bool IsAllowed, EventSequenceNumber SequenceNumber) _result;

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        var owner = EventSourceId.New();
        await _storage.Save(owner, _definition, 42L, "first");
        await _storage.Save(owner, _definition, 43L, "second");
    }

    async Task Because() => _result = await _storage.IsAllowed(EventSourceId.New(), _definition, "first");

    [Fact] void should_refuse_the_earlier_value() => _result.IsAllowed.ShouldBeFalse();
    [Fact] void should_identify_the_earlier_claim() => _result.SequenceNumber.ShouldEqual((EventSequenceNumber)42L);
}
