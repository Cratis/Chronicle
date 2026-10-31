// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class with_multiple_creator_events_for_a_derived_child : given.a_model_bound_projection_builder
{
    ChildrenDefinition _children;
    ChildrenDefinition _explicitChildren;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(PrimaryCreator), typeof(SecondaryCreator), typeof(ChildRemoved), typeof(ChildJoined)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because()
    {
        _children = builder.Build(typeof(Parent)).Children[nameof(Parent.Children)];
        _explicitChildren = builder.Build(typeof(ExplicitParent)).Children[nameof(ExplicitParent.Children)];
    }

    [Fact] void should_stamp_the_primary_creator() => From<PrimaryCreator>(_children).Properties["_derivedTypeId"].ShouldEqual("$value(child)");
    [Fact] void should_stamp_the_secondary_creator() => From<SecondaryCreator>(_children).Properties["_derivedTypeId"].ShouldEqual("$value(child)");
    [Fact] void should_not_create_a_from_subscription_for_removal() => _children.From.Keys.Any(key => key.IsEqual(Event<ChildRemoved>())).ShouldBeFalse();
    [Fact] void should_not_create_a_from_subscription_for_a_join() => _children.From.Keys.Any(key => key.IsEqual(Event<ChildJoined>())).ShouldBeFalse();
    [Fact] void should_preserve_the_join_subscription() => _children.Join.Keys.Any(key => key.IsEqual(Event<ChildJoined>())).ShouldBeTrue();
    [Fact] void should_preserve_the_removal_subscription() => _children.RemovedWith.Keys.Any(key => key.IsEqual(Event<ChildRemoved>())).ShouldBeTrue();
    [Fact] void should_preserve_an_explicit_discriminator() => From<SecondaryCreator>(_explicitChildren).Properties["_derivedTypeId"].ShouldEqual("$value(explicit)");

    FromDefinition From<TEvent>(ChildrenDefinition children) => children.From.Single(pair => pair.Key.IsEqual(Event<TEvent>())).Value;
    Cratis.Chronicle.Contracts.Events.EventType Event<TEvent>() => event_types.GetEventTypeFor(typeof(TEvent)).ToContract();

    [EventType]
    public record PrimaryCreator(Guid ParentId, Guid Id, string Name);

    [EventType]
    public record SecondaryCreator(Guid ParentId, Guid Id, string Name);

    [EventType]
    public record ChildRemoved(Guid ParentId, Guid Id);

    [EventType]
    public record ChildJoined(Guid Id, string Name);

    public interface IChild;

    [DerivedType("child", typeof(IChild))]
    [RemovedWith<ChildRemoved>(key: nameof(ChildRemoved.Id), parentKey: nameof(ChildRemoved.ParentId))]
    public record Child(
        [Key] Guid Id,
        [SetFrom<PrimaryCreator>]
        [SetFrom<SecondaryCreator>]
        [Join<ChildJoined>]
        string Name) : IChild;

    public record Parent(
        [Key] Guid Id,
        [ChildrenFrom<PrimaryCreator>(key: nameof(PrimaryCreator.Id), parentKey: nameof(PrimaryCreator.ParentId))]
        IReadOnlyList<Child> Children);

    [DerivedType("explicitChild", typeof(IChild))]
    public record ExplicitChild(
        [Key] Guid Id,
        [SetFrom<SecondaryCreator>] string Name,
        [SetValue<SecondaryCreator>("explicit")] string _derivedTypeId) : IChild;

    public record ExplicitParent(
        [Key] Guid Id,
        [ChildrenFrom<PrimaryCreator>(key: nameof(PrimaryCreator.Id), parentKey: nameof(PrimaryCreator.ParentId))]
        IReadOnlyList<ExplicitChild> Children);
}
