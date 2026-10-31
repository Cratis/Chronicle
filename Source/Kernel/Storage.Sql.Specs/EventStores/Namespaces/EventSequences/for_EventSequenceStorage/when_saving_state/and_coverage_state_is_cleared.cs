// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_saving_state;

public class and_coverage_state_is_cleared : given.a_state_storage
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    Task Establish() => _storage.SaveState(new() { ConstraintIndexWatermark = 10, ConstraintIndexCatchUpBelow = 20, ConstraintIndexCatchUpRefusing = ["unique"] });

    async Task Because()
    {
        await _storage.SaveState(new());
        _result = await _storage.GetState();
    }

    [Fact] void should_clear_the_watermark() => _result.ConstraintIndexWatermark.ShouldBeNull();
    [Fact] void should_clear_the_boundary() => _result.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_clear_the_refusal_marker() => _result.ConstraintIndexCatchUpRefusing.ShouldBeNull();
}
