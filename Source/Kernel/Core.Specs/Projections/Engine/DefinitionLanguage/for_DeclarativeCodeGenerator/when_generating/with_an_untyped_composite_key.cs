// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_an_untyped_composite_key : given.a_declarative_code_generator
{
    void Establish()
    {
        _readModelDefinition = CreateReadModelDefinition("Order", new Dictionary<string, JsonSchemaProperty>());
        var from = new Dictionary<EventType, FromDefinition>
        {
            [CreateEventType("OrderCreated")] = new FromDefinition(
                new Dictionary<PropertyPath, string>(),
                "$composite(customerId=customerId,orderNumber=orderNumber)",
                "$composite(customerId=customerId,orderNumber=orderNumber)")
        };
        _definition = CreateProjectionDefinition("OrderProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_use_the_fallback_type() => _result.ShouldContain(".UsingCompositeKey<CompositeKey>");
    [Fact] void should_keep_both_mappings() => _result.ShouldContain(".Set(k => k.orderNumber).To(e => e.orderNumber)");
    [Fact] void should_generate_the_parent_key() => _result.ShouldContain(".UsingParentCompositeKey<CompositeKey>");
}
