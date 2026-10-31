// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_unexpressible_every_operations : CodeGeneration.given.generated_schema_code
{
    void Establish()
    {
        _definition = CompileDeclaration(Subscription) with
        {
            FromEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string>
            {
                [new("ItemCount")] = "count",
                [new("Incremented")] = "increment",
                [new("Decremented")] = "decrement",
                [new("Added")] = "$add(Quantity)",
                [new("CustomerName")] = "CustomerName",
                [new("Dynamic.$eventContext.eventSourceId")] = "$eventContext(occurred)",
                [new("LastUpdated")] = "$eventContext(occurred)"
            },
            false)
        };
    }

    void Because() => _result = new CodeGeneration.CSharp.DeclarativeCodeGenerator().Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_explain_unexpressible_counter() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'ItemCount'");
    [Fact] void should_explain_unexpressible_increment() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'Incremented'");
    [Fact] void should_explain_unexpressible_decrement() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'Decremented'");
    [Fact] void should_explain_unexpressible_add() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'Added'");
    [Fact] void should_explain_unexpressible_event_property() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'CustomerName'");
    [Fact] void should_explain_unexpressible_dynamic_set() => _result.ShouldContain("Not expressible with the declarative builder: every-mapping 'Dynamic.$eventContext.eventSourceId'");
    [Fact] void should_keep_supported_context_mapping() => _result.ShouldContain(".Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)");
    [Fact] void should_keep_child_exclusion() => _result.ShouldContain(".ExcludeChildProjections()");
    [Fact] void should_compile() => CompileGenerated(_result, GenerateModelBound(Subscription)).ShouldNotBeNull();
}
