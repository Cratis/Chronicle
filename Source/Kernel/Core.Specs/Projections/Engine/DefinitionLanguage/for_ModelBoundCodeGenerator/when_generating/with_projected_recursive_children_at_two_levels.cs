// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_projected_recursive_children_at_two_levels : CodeGeneration.given.generated_schema_code
{
    const string Declaration = """
        projection Feature => Proof.Pdl.Feature
          from OrderPlaced
            Name = CustomerName
          children SubFeatures identified by Id
            from ProductRegistered
              Name = Name
            children SubFeatures identified by Id
              from ProductRenamed
                Name = Name
        """;

    void Establish() => UseSchema("""
        { "title": "Feature", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Name": { "type": "string" },
          "SubFeatures": { "type": "array", "items": { "$ref": "#" } }
        } }
        """);

    void Because() => _result = GenerateModelBound(Declaration);

    [Fact] void should_terminate_with_one_record() => CSharpSyntaxTree.ParseText(_result).GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Count().ShouldEqual(1);
    [Fact] void should_explain_the_conflict_on_the_shared_record() => CSharpSyntaxTree.ParseText(_result).GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Single().GetLeadingTrivia().ToFullString().ShouldContain("Not expressible with model-bound attributes: conflicting projection scopes for record 'Feature'");
    [Fact] void should_compile() => CompileGenerated(GenerateDeclarative(Declaration), _result).ShouldNotBeNull();
}
