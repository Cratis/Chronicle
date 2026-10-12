// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_child_join_on_a_foreign_key : CodeGeneration.given.generated_child_join_code
{
    Contracts.Projections.ProjectionDefinition _declarative;

    void Because()
    {
        _result = GenerateDeclarative(Declaration);
        var assembly = CompileGenerated(_result, GenerateModelBound(Declaration), ChildJoinEvents);
        _declarative = CodeGeneration.given.generated_projection_definitions.Declarative(assembly);
    }

    [Fact] void should_explain_the_unexpressible_join() => _result.ShouldContain("Not expressible with the declarative builder: child join 'ProductRenamed' on 'sku' differs from identity 'lineNumber'");
    [Fact] void should_not_emit_a_join_with_different_meaning() => _result.ShouldNotContain(".Join<ProductRenamed>");
    [Fact] void should_not_specify_an_unsupported_join_target() => _result.ShouldNotContain(".On(");
    [Fact] void should_not_build_a_join_on_the_child_identity() => _declarative.Children["lines"].Join.ShouldBeEmpty();
    [Fact] void should_keep_the_child_subscription() => _declarative.Children["lines"].From.Single().Key.Id.ShouldEqual("LineAdded");
}
