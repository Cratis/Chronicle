// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_a_child_precedes_root_creation : given.an_order_projection_grain_with_lines_joined_to_products
{
    protected override string Declaration => """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
            CustomerName = CustomerName
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
              parent $eventContext.eventSourceId
              Quantity = Quantity
        """;

    AppendedEvent _child;
    AppendedEvent _created;
    IEnumerable<ExpandoObject> _beforeCreation;
    IEnumerable<ExpandoObject> _previewBeforeCreation;
    IEnumerable<ExpandoObject> _afterCreation;
    IEnumerable<ExpandoObject> _previewAfterCreation;

    async Task Establish()
    {
        _child = await ItemAddedToOrder(Kettle, 2);
        _created = await OrderPlaced("Ada");
    }

    async Task Because()
    {
        _beforeCreation = await _grain.Process(EventStoreNamespaceName.Default, [_child]);
        _previewBeforeCreation = await Preview(_child);
        _afterCreation = await _grain.Process(EventStoreNamespaceName.Default, [_child, _created]);
        _previewAfterCreation = await Preview(_child, _created);
    }

    [Fact] void should_not_return_a_root_before_creation() => _beforeCreation.ShouldBeEmpty();
    [Fact] void should_not_preview_a_root_before_creation() => _previewBeforeCreation.ShouldBeEmpty();
    [Fact] void should_return_the_root_after_creation() => _afterCreation.Count().ShouldEqual(1);
    [Fact] void should_preview_the_root_after_creation() => _previewAfterCreation.Count().ShouldEqual(1);
    [Fact] void should_keep_the_child_after_creation() => LinesOf(ReadModelFor(_afterCreation, Order)).Single()["Quantity"].ShouldEqual(2);
    [Fact] void should_preview_the_child_after_creation() => LinesOf(ReadModelFor(_previewAfterCreation, Order)).Single()["Quantity"].ShouldEqual(2);
}
