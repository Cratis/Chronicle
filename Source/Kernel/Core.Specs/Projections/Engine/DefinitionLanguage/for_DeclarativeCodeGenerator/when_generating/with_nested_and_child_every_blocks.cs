// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_nested_and_child_every_blocks : CodeGeneration.given.generated_order_code
{
    void Because() => _result = GenerateDeclarative(NestedAndChildEvery);

    [Fact] void should_generate_nested_builder() => _result.ShouldContain(".Nested(m => m.Details");
    [Fact] void should_clear_nested_object() => _result.ShouldContain(".RemovedWith<OrderCancelled>()");
    [Fact] void should_keep_child_every_mapping() => _result.ShouldContain(".FromEvery(_ => _.Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)");
    [Fact] void should_keep_child_removal_via_join() => _result.ShouldContain(".RemovedWithJoin<ItemRemovedFromOrder>(_ => _.UsingKey(e => e.ProductId))");
    [Fact] void should_compile() => ShouldCompile(_result, GenerateModelBound(NestedAndChildEvery));
}
