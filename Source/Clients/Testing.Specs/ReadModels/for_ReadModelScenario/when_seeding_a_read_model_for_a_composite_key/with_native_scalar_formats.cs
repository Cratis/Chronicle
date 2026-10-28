// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_native_scalar_formats : Specification
{
    readonly NativeSeedKey _key = new(Guid.Parse("423f0492-56de-47cb-bb92-1d8915bf458b"), 42, 9007199254740993L, true, new DateOnly(2026, 10, 28), new DateTimeOffset(2026, 10, 28, 14, 15, 16, TimeSpan.FromHours(2)));
    ReadModelScenario<NativeSeedModel> _scenario;
    NativeSeedModel _result;

    void Establish() => _scenario = new ReadModelScenario<NativeSeedModel>().WithProjection(builder => builder
        .From<NativeSeedAdded>(from => from
            .UsingCompositeKey<NativeSeedKey>(key => key
                .Set(part => part.Identifier).To(@event => @event.Identifier)
                .Set(part => part.Number).To(@event => @event.Number)
                .Set(part => part.Large).To(@event => @event.Large)
                .Set(part => part.Enabled).To(@event => @event.Enabled)
                .Set(part => part.Day).To(@event => @event.Day)
                .Set(part => part.Occurred).To(@event => @event.Occurred))
            .Increment(model => model.Count)));

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("source").Events(new NativeSeedAdded(_key.Identifier, _key.Number, _key.Large, _key.Enabled, _key.Day, _key.Occurred));
        _result = _scenario.Instance!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_preserve_guid() => _result.Id.Identifier.ShouldEqual(_key.Identifier);
    [Fact] void should_preserve_integer() => _result.Id.Number.ShouldEqual(_key.Number);
    [Fact] void should_preserve_long_precision() => _result.Id.Large.ShouldEqual(_key.Large);
    [Fact] void should_preserve_boolean() => _result.Id.Enabled.ShouldEqual(_key.Enabled);
    [Fact] void should_preserve_date() => _result.Id.Day.ShouldEqual(_key.Day);
    [Fact] void should_preserve_timestamp() => _result.Id.Occurred.ShouldEqual(_key.Occurred);
}
