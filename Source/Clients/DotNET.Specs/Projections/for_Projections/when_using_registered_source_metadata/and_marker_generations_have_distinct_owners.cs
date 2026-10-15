// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_marker_generations_have_distinct_owners : an_explicit_store
{
    string _modelBoundSequence;
    string _fluentSequence;
    string _wireOwner;

    void Establish()
    {
        _options.EventGenerationDelivery = EventGenerationDelivery.Pinned;
        _artifacts.EventTypes.Returns([typeof(Previous), typeof(Current)]);
        _artifacts.ModelBoundProjections.Returns([typeof(Model)]);
        _artifacts.Projections.Returns([typeof(FluentProjection)]);
        _activator.ActivateNonDisposable<IProjectionFor<FluentModel>>(typeof(FluentProjection)).Returns(new FluentProjection());
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        var definitions = ((Projections)_store.Projections).Definitions;
        _modelBoundSequence = definitions.Single(_ => _.ReadModel == typeof(Model).FullName).EventSequenceId;
        _fluentSequence = definitions.Single(_ => _.ReadModel == typeof(FluentModel).FullName).EventSequenceId;
        await _store.EventTypes.Register();
        _wireOwner = _payload.Types.Single().EventStore;
    }

    [Fact] void should_keep_legacy_model_bound_source_selection_on_the_latest_generation() => _modelBoundSequence.ShouldEqual("inbox-current-owner");
    [Fact] void should_keep_wire_registration_on_the_latest_generations_owner() => _wireOwner.ShouldEqual("current-owner");
    [Fact] void should_keep_legacy_fluent_source_selection_on_the_declared_generation() => _fluentSequence.ShouldEqual("inbox-previous-owner");

    [EventType("conflicting-owner-event", generation: 1), EventStore("previous-owner")]
    public record Previous(string Name);
    [EventType("conflicting-owner-event", generation: 2), EventStore("current-owner")]
    public record Current(string Name);
    [FromEvent<Previous>]
    public record Model(string Name);
    public record FluentModel(string Name);
    public class FluentProjection : IProjectionFor<FluentModel>
    {
        public void Define(IProjectionBuilderFor<FluentModel> builder) => builder.From<Previous>();
    }
}
