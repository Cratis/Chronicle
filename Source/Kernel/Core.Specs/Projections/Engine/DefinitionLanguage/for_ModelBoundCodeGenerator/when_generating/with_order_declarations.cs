// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_order_declarations : CodeGeneration.given.generated_order_code
{
    void Because() => _result = GenerateModelBound(FullOrder);

    [Fact] void should_map_every_event_context() => _result.ShouldContain("[FromEvery(contextProperty: \"Occurred\")]");
    [Fact] void should_generate_children_subscription() => _result.ShouldContain("ChildrenFrom<ItemAddedToOrder>");
    [Fact] void should_generate_children_update() => _result.ShouldContain("ChildrenFrom<ItemQuantityAdjusted>");
    [Fact] void should_remove_order() => _result.ShouldContain("[RemovedWith<OrderCancelled>]");
    [Fact] void should_remove_child() => _result.ShouldContain("RemovedWith<ItemRemovedFromOrder>");
    [Fact] void should_join_registered_product_name() => _result.ShouldContain("Join<ProductRegistered>(eventPropertyName: nameof(ProductRegistered.Name))");
    [Fact] void should_join_renamed_product_name() => _result.ShouldContain("Join<ProductRenamed>(eventPropertyName: nameof(ProductRenamed.Name))");
    [Fact] void should_type_timestamp() => _result.ShouldContain("System.DateTimeOffset LastUpdated");
    [Fact] void should_type_identifier() => _result.ShouldContain("System.Guid Id");
    [Fact] void should_type_child_collection() => _result.ShouldContain("System.Collections.Generic.IEnumerable<OrderLine> Lines");
    [Fact] void should_emit_child_record() => _result.ShouldContain("public record OrderLine(");
    [Fact] void should_close_record() => _result.TrimEnd().EndsWith(';').ShouldBeTrue();
}
