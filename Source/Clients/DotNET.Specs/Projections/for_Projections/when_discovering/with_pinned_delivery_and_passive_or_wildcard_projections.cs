// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class with_pinned_delivery_and_passive_or_wildcard_projections : given.all_dependencies
{
    public record PassiveModel();
    public record WildcardModel();

    public class PassiveProjection : IProjectionFor<PassiveModel>
    {
        public void Define(IProjectionBuilderFor<PassiveModel> builder) => builder.Passive();
    }

    public class WildcardProjection : IProjectionFor<WildcardModel>
    {
        public void Define(IProjectionBuilderFor<WildcardModel> builder) => builder.FromAll(_ => { });
    }

    Projections _projections;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(PassiveProjection), typeof(WildcardProjection)]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<PassiveModel>>(typeof(PassiveProjection)).Returns(new PassiveProjection());
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<WildcardModel>>(typeof(WildcardProjection)).Returns(new WildcardProjection());
        _projections = new(_eventStore, _eventTypes, _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance) { GenerationDelivery = EventGenerationDelivery.Pinned };
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_keep_passive_delivery_unchanged() => _projections.Definitions.Single(definition => !definition.IsActive).GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_keep_wildcard_delivery_unchanged() => _projections.Definitions.Single(definition => definition.SubscribesToAllEvents).GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
}
