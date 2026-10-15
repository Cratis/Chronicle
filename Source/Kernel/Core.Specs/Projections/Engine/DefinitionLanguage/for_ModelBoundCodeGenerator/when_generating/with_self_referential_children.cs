// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_self_referential_children : CodeGeneration.given.generated_schema_code
{
    void Establish() => UseSchema("""
        { "title": "Order", "type": "object", "properties": {
          "Id": { "type": "string" },
          "Features": { "type": "array", "items": {
            "title": "Feature", "type": "object", "properties": {
              "Id": { "type": "string", "format": "guid" },
              "Name": { "type": "string" },
              "SubFeatures": { "type": "array", "items": { "$ref": "#/properties/Features/items" } }
            }
          } }
        } }
        """);

    void Because() => _result = GenerateModelBound(Subscription);

    [Fact] void should_emit_the_feature_record_once() => CSharpSyntaxTree.ParseText(_result).GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Count(record => record.Identifier.Text == "Feature").ShouldEqual(1);
    [Fact] void should_reuse_the_record_in_the_recursive_member() => _result.ShouldContain("System.Collections.Generic.IEnumerable<Feature> SubFeatures");
    [Fact] void should_compile_and_terminate() => ShouldCompile(GenerateDeclarative(Subscription), _result);
}
