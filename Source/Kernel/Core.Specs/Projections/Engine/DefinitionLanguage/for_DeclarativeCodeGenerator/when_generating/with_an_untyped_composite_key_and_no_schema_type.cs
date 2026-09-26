// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_an_untyped_composite_key_and_no_schema_type : given.a_declarative_code_generator
{
    void Establish()
    {
        _readModelDefinition = CreateReadModelDefinition("Order", new() { ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String } });
        _definition = CreateProjectionDefinition(
            "OrderProjection",
            _readModelDefinition.Identifier,
            from: new Dictionary<EventType, FromDefinition>
            {
                [CreateEventType("OrderCreated")] = new FromDefinition(
                    new Dictionary<PropertyPath, string>(),
                    "$composite(first=first,second=second)",
                    null)
            });
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_use_a_fallback_type() => _result.ShouldContain(".UsingCompositeKey<CompositeKey>");
    [Fact] void should_generate_both_mappings() => _result.ShouldContain(".Set(k => k.second).To(e => e.second)");
}
