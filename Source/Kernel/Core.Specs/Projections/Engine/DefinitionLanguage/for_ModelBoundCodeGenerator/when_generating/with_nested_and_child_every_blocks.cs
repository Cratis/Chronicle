// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_nested_and_child_every_blocks : CodeGeneration.given.generated_order_code
{
    Contracts.Projections.ProjectionDefinition _modelBound;

    void Because()
    {
        var assembly = CompileGenerated(GenerateDeclarative(NestedAndChildEvery), GenerateModelBound(NestedAndChildEvery));
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_keep_child_every_mapping_in_the_child() => _modelBound.Children["lines"].From.Single().Value.Properties["lastUpdated"].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_not_promote_child_every_mapping_to_the_root() => _modelBound.All.Properties.ShouldBeEmpty();
    [Fact] void should_keep_nested_object() => _modelBound.Nested.Keys.ShouldContain("details");
    [Fact] void should_clear_nested_object() => _modelBound.Nested["details"].RemovedWith.Single().Key.Id.ShouldEqual("OrderCancelled");
    [Fact] void should_keep_child_removal_via_join() => _modelBound.Children["lines"].RemovedWithJoin.Single().Value.Key.ShouldEqual("productId");
}
