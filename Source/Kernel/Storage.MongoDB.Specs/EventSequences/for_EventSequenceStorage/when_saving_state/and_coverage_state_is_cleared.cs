// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_saving_state;

[Collection(MongoDBCollection.Name)]
public class and_coverage_state_is_cleared(MongoDBFixture fixture) : given.a_state_storage(fixture)
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
