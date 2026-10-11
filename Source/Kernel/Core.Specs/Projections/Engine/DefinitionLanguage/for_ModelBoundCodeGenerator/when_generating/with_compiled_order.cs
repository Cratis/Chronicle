// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_compiled_order : CodeGeneration.given.generated_order_code
{
    Contracts.Projections.ProjectionDefinition _declarative;
    Contracts.Projections.ProjectionDefinition _modelBound;

    void Because()
    {
        var assembly = CompileGenerated(GenerateDeclarative(FullOrder), GenerateModelBound(FullOrder));
        _declarative = CodeGeneration.given.generated_projection_definitions.Declarative(assembly);
        _modelBound = CodeGeneration.given.generated_projection_definitions.ModelBound(assembly);
    }

    [Fact] void should_keep_root_subscriptions() => _modelBound.From.Keys.Select(eventType => eventType.Id).ShouldContainOnly(_declarative.From.Keys.Select(eventType => eventType.Id));
    [Fact] void should_keep_every_mapping() => _modelBound.All.Properties["lastUpdated"].ShouldEqual(_declarative.All.Properties["lastUpdated"]);
    [Fact] void should_not_subscribe_to_all_events() => _modelBound.SubscribesToAllEvents.ShouldBeFalse();
    [Fact] void should_keep_child_subscriptions() => _modelBound.Children["lines"].From.Keys.Select(eventType => eventType.Id).ShouldContainOnly(_declarative.Children["lines"].From.Keys.Select(eventType => eventType.Id));
    [Fact] void should_keep_child_identity() => _modelBound.Children["lines"].IdentifiedBy.ShouldEqual(_declarative.Children["lines"].IdentifiedBy);
    [Fact] void should_keep_child_joins() => _modelBound.Children["lines"].Join.Keys.Select(eventType => eventType.Id).ShouldContainOnly(_declarative.Children["lines"].Join.Keys.Select(eventType => eventType.Id));
    [Fact] void should_join_children_on_the_child_identity() => _modelBound.Children["lines"].Join.Single(join => join.Key.Id == "ProductRegistered").Value.On.ShouldEqual(_declarative.Children["lines"].Join.Single(join => join.Key.Id == "ProductRegistered").Value.On);
    [Fact] void should_keep_child_join_mapping() => _modelBound.Children["lines"].Join.Single(join => join.Key.Id == "ProductRegistered").Value.Properties["productName"].ShouldEqual("name");
    [Fact] void should_keep_child_removal_key() => _modelBound.Children["lines"].RemovedWith.Single().Value.Key.ShouldEqual(_declarative.Children["lines"].RemovedWith.Single().Value.Key);
    [Fact] void should_remove_child_in_the_event_source_parent() => _modelBound.Children["lines"].RemovedWith.Single().Value.ParentKey.ShouldEqual("$eventSourceId");
    [Fact] void should_use_event_source_context_for_declarative_child_removal_parent() => _declarative.Children["lines"].RemovedWith.Single().Value.ParentKey.ShouldEqual("$eventContext(eventSourceId)");
    [Fact] void should_remove_only_the_order_on_cancellation() => _modelBound.RemovedWith.Keys.Select(eventType => eventType.Id).ShouldContainOnly(["OrderCancelled"]);
}
