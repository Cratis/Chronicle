// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

public class a_native_projection : Specification
{
    protected readonly NativeSeedKey _key = new(Guid.Parse("423f0492-56de-47cb-bb92-1d8915bf458b"), 42, 9007199254740993L, true, new DateOnly(2026, 10, 28), new DateTimeOffset(2026, 10, 28, 14, 15, 16, TimeSpan.FromHours(2)), SeedStatus.Active, new SeedCode("code"));
    protected ReadModelScenario<NativeSeedModel> _scenario;
    protected Dictionary<string, object?> _parts;

    void Establish()
    {
        _scenario = new ReadModelScenario<NativeSeedModel>().WithProjection(builder => builder
            .From<NativeSeedAdded>(from => from
                .UsingCompositeKey<NativeSeedKey>(key => key
                    .Set(part => part.Identifier).To(@event => @event.Identifier)
                    .Set(part => part.Number).To(@event => @event.Number)
                    .Set(part => part.Large).To(@event => @event.Large)
                    .Set(part => part.Enabled).To(@event => @event.Enabled)
                    .Set(part => part.Day).To(@event => @event.Day)
                    .Set(part => part.Occurred).To(@event => @event.Occurred)
                    .Set(part => part.Status).To(@event => @event.Status)
                    .Set(part => part.Code).To(@event => @event.Code))
                .Increment(model => model.Count)));
        _parts = new()
        {
            ["identifier"] = _key.Identifier,
            ["number"] = _key.Number,
            ["large"] = _key.Large,
            ["enabled"] = _key.Enabled,
            ["day"] = _key.Day,
            ["occurred"] = _key.Occurred,
            ["status"] = _key.Status,
            ["code"] = _key.Code
        };
    }

    protected NativeSeedAdded Event() => new(_key.Identifier, _key.Number, _key.Large, _key.Enabled, _key.Day, _key.Occurred, _key.Status, _key.Code);

    void Destroy() => _scenario.Dispose();
}
