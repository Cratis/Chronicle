// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_supported_all_counter : CodeGeneration.given.generated_schema_code
{
    Contracts.Projections.ProjectionDefinition _declarative;

    void Establish()
    {
        _definition = CompileDeclaration(Subscription) with
        {
            SubscribesToAllEvents = true,
            FromEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string> { [new("ItemCount")] = "count" }, true)
        };
    }

    void Because()
    {
        _result = new CodeGeneration.CSharp.DeclarativeCodeGenerator().Generate(_definition, _readModelDefinition).ToFullString();
        var assembly = CompileGenerated(_result, GenerateModelBound(Subscription));
        _declarative = CodeGeneration.given.generated_projection_definitions.Declarative(assembly);
    }

    [Fact] void should_keep_supported_all_counter() => _result.ShouldContain(".FromAll(_ => _.Count(m => m.ItemCount))");
    [Fact] void should_subscribe_to_all_events() => _declarative.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_keep_counter_in_the_builder_definition() => _declarative.All.Properties["itemCount"].ShouldEqual("$count");
}
