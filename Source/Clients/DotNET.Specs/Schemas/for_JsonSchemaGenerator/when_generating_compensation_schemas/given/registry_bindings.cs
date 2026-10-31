// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas.given;

public class registry_bindings : for_JsonSchemaGenerator.given.a_json_schema_generator
{
    protected IEventTypes _first;
    protected IEventTypes _second;

    void Establish()
    {
        _first = Substitute.For<IEventTypes>();
        _first.HasFor(typeof(PlainEvent)).Returns(true);
        _first.GetEventTypeFor(typeof(PlainEvent)).Returns(new EventType("first-alias", 1));
        _second = Substitute.For<IEventTypes>();
        _second.HasFor(typeof(PlainEvent)).Returns(true);
        _second.GetEventTypeFor(typeof(PlainEvent)).Returns(new EventType("second-alias", 1));
    }

    public record PlainEvent(string Value);

    [CompensationFor<PlainEvent>]
    public record Compensation(string Value = "default");

    public sealed class CustomGenerator : IJsonSchemaGenerator
    {
        public JsonSchema Precise { get; } = JsonSchema.FromJson("{\"custom\":\"precise\",\"x-compensation-for\":\"owned-by-custom-generator\"}");

        public JsonSchema Legacy { get; } = JsonSchema.FromJson("{\"custom\":\"legacy\",\"x-compensation-for\":\"owned-by-custom-generator\"}");

        public int PreciseCalls { get; private set; }

        public int LegacyCalls { get; private set; }

        public JsonSchema Generate(Type type)
        {
            PreciseCalls++;
            return Precise;
        }

        public JsonSchema GenerateLegacyEventType(Type type)
        {
            LegacyCalls++;
            return Legacy;
        }
    }
}
