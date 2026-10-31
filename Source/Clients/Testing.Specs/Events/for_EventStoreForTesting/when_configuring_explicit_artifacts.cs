// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_configuring_explicit_artifacts : Specification
{
    EventStoreForTesting _store;
    ChronicleOptions _options;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ThingTouched)] };
        _options = new ChronicleOptions();
        _options.ExplicitArtifacts.RegisterProjection<ExplicitNote>(builder => builder.From<ThingTouched>());
        _options.ExplicitArtifacts.RegisterReactor("explicit-reactor", builder => builder.On<ThingTouched>((_, _) => { }));
        _store = new(null, artifacts, _options);
    }

    Task Because() => _store.DiscoverAll();

    [Fact] void should_discover_the_explicit_projection() => _store.Projections.GetProjectionIdForModel<ExplicitNote>().ShouldEqual(new Chronicle.Projections.ProjectionId(typeof(ExplicitNote).FullName!));
    [Fact] void should_discover_the_explicit_reactor() => _store.Reactors.GetHandlerById("explicit-reactor").Id.ShouldEqual(new ReactorId("explicit-reactor"));
}
