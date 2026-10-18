// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_more_events_after_reading : Specification
{
    ReadModelScenario<SeededKeyModel> _scenario;
    SeededKeyModel _result;

    async Task Establish()
    {
        _scenario = new();
        await _scenario.Given.ForEventSourceId("invoice-1").ReadModel(new("invoice-1", "before", "retained", 5));
        await _scenario.Given.ForEventSourceId("invoice-1").Events(new SeededKeyRenamed("first"));
        _ = _scenario.Instance;
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("invoice-1").Events(new SeededKeyRenamed("second"));
        _result = _scenario.Instance!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_replay_from_the_original_seed_without_double_counting() => _result.Count.ShouldEqual(7);
    [Fact] void should_apply_the_latest_event() => _result.Name.ShouldEqual("second");
}
