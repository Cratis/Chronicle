// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_read_model_string_lengths : given.a_json_schema_generator
{
    record Model([property: MaxLength(80)] string Bounded, [property: StringLength(120)] string? Optional, [property: Length(0, 64)] string Sized, string Unbounded);

    JsonSchema _readModel;
    JsonSchema _event;
    JsonSchema _legacyEvent;

    void Because()
    {
        _readModel = _generator.GenerateForReadModel(typeof(Model));
        _event = _generator.Generate(typeof(Model));
        _legacyEvent = _generator.GenerateLegacyEventType(typeof(Model));
    }

    [Fact] void should_emit_an_explicit_maximum_length() => _readModel.Properties["Bounded"].MaxLength.ShouldEqual(80);
    [Fact] void should_emit_an_explicit_nullable_string_length() => _readModel.Properties["Optional"].MaxLength.ShouldEqual(120);
    [Fact] void should_emit_an_explicit_length_attribute() => _readModel.Properties["Sized"].MaxLength.ShouldEqual(64);
    [Fact] void should_not_invent_a_default_length() => _readModel.Properties["Unbounded"].MaxLength.ShouldBeNull();
    [Fact] void should_not_change_event_schemas() => _event.Properties["Bounded"].MaxLength.ShouldBeNull();
    [Fact] void should_not_change_legacy_event_schemas() => _legacyEvent.Properties["Bounded"].MaxLength.ShouldBeNull();
}
