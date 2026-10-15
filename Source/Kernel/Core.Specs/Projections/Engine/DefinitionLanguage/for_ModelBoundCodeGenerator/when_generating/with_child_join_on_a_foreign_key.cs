// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_child_join_on_a_foreign_key : CodeGeneration.given.generated_child_join_code
{
    Contracts.Projections.ProjectionDefinition _modelBound;

    void Because()
    {
        _result = GenerateModelBound(Declaration);
        var assembly = CompileGenerated(GenerateDeclarative(Declaration), _result, ChildJoinEvents);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_specify_the_join_target() => _result.ShouldContain("Join<ProductRenamed>(on: \"sku\", eventPropertyName: nameof(ProductRenamed.sku))");
    [Fact] void should_keep_the_join_target_in_the_client_definition() => _modelBound.Children["lines"].Join.Single().Value.On.ShouldEqual("sku");
    [Fact] void should_keep_the_distinct_child_identity() => _modelBound.Children["lines"].IdentifiedBy.ShouldEqual("lineNumber");
}
