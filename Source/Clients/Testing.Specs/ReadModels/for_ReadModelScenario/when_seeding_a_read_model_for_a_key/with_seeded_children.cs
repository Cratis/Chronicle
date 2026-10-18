// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_model_bound_child;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_seeded_children : Specification
{
    readonly EventSourceId _key = EventSourceId.New();
    readonly Guid _firstProduct = Guid.NewGuid();
    readonly Guid _secondProduct = Guid.NewGuid();
    ReadModelScenario<Order> _scenario;
    Order _result;

    void Establish()
    {
        _scenario = new ReadModelScenario<Order>().WithProjection(builder => builder
            .Children(model => model.Lines, children => children
                .IdentifiedBy(line => line.ProductId)
                .From<ItemAddedToOrder>(from => from
                    .UsingKey(@event => @event.ProductId)
                    .UsingParentKeyFromContext(context => context.EventSourceId))));
        _scenario.Given.ForEventSourceId(_key).ReadModel(new(Guid.Parse(_key.Value), "seeded customer", [new(_firstProduct, 5, "seeded product")]));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId(_key).Events(new ItemAddedToOrder(_secondProduct, 2));
        _result = _scenario.Instance!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_retain_the_seeded_child() => _result.Lines.Single(line => line.ProductId == _firstProduct).Quantity.ShouldEqual(5);
    [Fact] void should_add_the_new_child_exactly_once() => _result.Lines.Select(line => line.ProductId).ShouldContainOnly(_firstProduct, _secondProduct);
    [Fact] void should_preserve_the_seeded_parent() => _result.CustomerName.ShouldEqual("seeded customer");
}
