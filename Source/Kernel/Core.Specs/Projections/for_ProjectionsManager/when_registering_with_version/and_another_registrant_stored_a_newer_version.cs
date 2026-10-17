// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_another_registrant_stored_a_newer_version : given.a_projections_manager_grain
{
    ProjectionDefinition _registered;
    ProjectionDefinition _omitted;
    ProjectionDefinition _otherApplications;

    void Establish()
    {
        _registered = CreateDefinition("registered-projection", "registered-read-model") with { Version = "2.0.0" };
        _omitted = CreateDefinition("omitted-projection", "omitted-read-model") with { Version = "1.0.0" };
        _otherApplications = CreateDefinition("other-projection", "other-read-model") with { Version = "10.0.0" };
        _state.Projections = [_registered, _omitted, _otherApplications];
        _state.Registrants = new Dictionary<string, string>
        {
            ["registered-projection"] = "the-app",
            ["omitted-projection"] = "the-app",
            ["other-projection"] = "the-other-app"
        };
        _readModelDefinitions =
        [
            CreateReadModelDefinition("registered-read-model"),
            CreateReadModelDefinition("omitted-read-model", "omittedContainer"),
            CreateReadModelDefinition("other-read-model", "otherContainer")
        ];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    Task Because() => _grain.Register([_registered], ProjectionOwner.Client, "the-app", "2.0.0");

    [Fact] void should_retire_the_projection_it_no_longer_registers() => _state.Projections.ShouldNotContain(_omitted);
    [Fact] void should_keep_the_other_applications_projection() => _state.Projections.ShouldContain(_otherApplications);
    [Fact] async Task should_retire_one_observer() => await _observerGrain.Received(1).Retire();
}
