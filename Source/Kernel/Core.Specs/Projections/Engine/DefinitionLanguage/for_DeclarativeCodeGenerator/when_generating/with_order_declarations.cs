// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_order_declarations : CodeGeneration.given.generated_order_code
{
    string _minimal;
    string _childJoin;
    string _full;

    void Because()
    {
        _minimal = GenerateDeclarative(MinimalRemoval);
        _childJoin = GenerateDeclarative(ChildJoin);
        _full = GenerateDeclarative(FullOrder);
    }

    [Fact] void should_compile_minimal_removal() => ShouldCompile(_minimal, GenerateModelBound(MinimalRemoval));
    [Fact] void should_compile_child_join() => ShouldCompile(_childJoin, GenerateModelBound(ChildJoin));
    [Fact] void should_compile_full_order() => ShouldCompile(_full, GenerateModelBound(FullOrder));
    [Fact] void should_remove_order_without_a_key_lambda() => _minimal.ShouldContain(".RemovedWith<OrderCancelled>()");
    [Fact] void should_not_emit_on_for_child_joins() => _childJoin.ShouldNotContain(".On(");
    [Fact] void should_map_every_event() => _full.ShouldContain(".FromEvery(");
    [Fact] void should_remove_child_by_product() => _full.ShouldContain(".RemovedWith<ItemRemovedFromOrder>(_ => _.UsingKey(e => e.ProductId)");
    [Fact] void should_use_pascal_case_context_paths() => _full.ShouldContain(".UsingParentKeyFromContext(c => c.EventSourceId)");
    [Fact] void should_keep_product_rename_join() => _full.ShouldContain(".Join<ProductRenamed>");
    [Fact] void should_keep_item_count_increment() => _full.ShouldContain(".Increment(m => m.ItemCount)");
    [Fact] void should_keep_item_count_decrement() => _full.ShouldContain(".Decrement(m => m.ItemCount)");
}
