// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_an_unprojected_root_self_reference : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Feature => Proof.Pdl.Feature
          from OrderPlaced
            Name = CustomerName
        """;

    Contracts.Projections.ProjectionDefinition _modelBound;

    void Establish() => UseSchema("""
        { "title": "Feature", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Name": { "type": "string" },
          "SubFeatures": { "type": "array", "items": { "$ref": "#" } }
        } }
        """);

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly, "Feature");
    }

    [Fact] void should_emit_the_root_record_once() => CSharpSyntaxTree.ParseText(_result).GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Count(record => record.Identifier.Text == "Feature").ShouldEqual(1);
    [Fact] void should_keep_the_root_subscription_attribute() => _result.ShouldContain("[FromEvent<OrderPlaced>]");
    [Fact] void should_keep_the_root_mapping_attribute() => _result.ShouldContain("[SetFrom<OrderPlaced>(nameof(OrderPlaced.CustomerName))]");
    [Fact] void should_not_report_a_false_conflict() => _result.ShouldNotContain("conflicting projection scopes");
    [Fact] void should_keep_the_subscription_in_the_builder_definition() => _modelBound.From.Single().Key.Id.ShouldEqual("OrderPlaced");
    [Fact] void should_keep_the_mapping_in_the_builder_definition() => _modelBound.From.Single().Value.Properties["name"].ShouldEqual("customerName");
    [Fact] void should_not_project_the_unannotated_recursive_member() => _modelBound.Children.ShouldBeEmpty();
}
