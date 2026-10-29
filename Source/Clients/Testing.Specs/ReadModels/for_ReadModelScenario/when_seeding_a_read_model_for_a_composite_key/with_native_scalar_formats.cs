// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_native_scalar_formats : a_native_projection
{
    NativeSeedModel _result;

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("source").Events(Event());
        _result = _scenario.Instance!;
    }

    [Fact] void should_preserve_enum() => _result.Id.Status.ShouldEqual(_key.Status);
    [Fact] void should_preserve_concept() => _result.Id.Code.ShouldEqual(_key.Code);
    [Fact] void should_preserve_guid() => _result.Id.Identifier.ShouldEqual(_key.Identifier);
    [Fact] void should_preserve_integer() => _result.Id.Number.ShouldEqual(_key.Number);
    [Fact] void should_preserve_long_precision() => _result.Id.Large.ShouldEqual(_key.Large);
    [Fact] void should_preserve_boolean() => _result.Id.Enabled.ShouldEqual(_key.Enabled);
    [Fact] void should_preserve_date() => _result.Id.Day.ShouldEqual(_key.Day);
    [Fact] void should_preserve_timestamp() => _result.Id.Occurred.ShouldEqual(_key.Occurred);
}
