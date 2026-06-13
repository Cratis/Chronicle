// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class with_nested_object_in_children_collection : Specification
{
    ProjectionBuilderFor<FeatureReadModel> _builder;
    IEventTypes _eventTypes;
    ProjectionDefinition _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(SliceAddedToFeature), typeof(CommandSetOnSlice), typeof(CommandClearedFromSlice)]);
        _builder = new ProjectionBuilderFor<FeatureReadModel>(
            new ProjectionId(typeof(FeatureReadModel).FullName),
            typeof(FeatureReadModel),
            new DefaultNamingPolicy(),
            _eventTypes,
            new JsonSerializerOptions());
    }

    void Because()
    {
        _builder.Children(_ => _.Slices, slices => slices
            .IdentifiedBy(_ => _.Id)
            .From<SliceAddedToFeature>(b => b
                .UsingParentKey(e => e.FeatureId)
                .UsingKey(e => e.SliceId))
            .Nested(_ => _.Command, nested => nested
                .From<CommandSetOnSlice>()
                .ClearWith<CommandClearedFromSlice>()));

        _result = _builder.Build();
    }

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_entry_for_slices() => _result.Children.Keys.ShouldContain(nameof(FeatureReadModel.Slices));
    [Fact] void should_have_nested_entry_for_command_in_children() => _result.Children[nameof(FeatureReadModel.Slices)].Nested.Keys.ShouldContain(nameof(SliceReadModel.Command));

    [Fact]
    void should_have_from_definition_for_nested_command_set()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(CommandSetOnSlice)).ToContract();
        var nestedDefinition = _result.Children[nameof(FeatureReadModel.Slices)].Nested[nameof(SliceReadModel.Command)];
        nestedDefinition.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_removed_with_definition_for_nested_command_clear()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(CommandClearedFromSlice)).ToContract();
        var nestedDefinition = _result.Children[nameof(FeatureReadModel.Slices)].Nested[nameof(SliceReadModel.Command)];
        nestedDefinition.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }
}

[EventType]
public record SliceAddedToFeature(Guid FeatureId, Guid SliceId, string Name);

[EventType]
public record CommandSetOnSlice(string Name, string Schema);

[EventType]
public record CommandClearedFromSlice;

public record SliceCommandReadModel(string Name, string Schema);

public record SliceReadModel(Guid Id, string Name, SliceCommandReadModel? Command);

public record FeatureReadModel(IEnumerable<SliceReadModel> Slices);

#pragma warning restore SA1402 // File may only contain a single type
