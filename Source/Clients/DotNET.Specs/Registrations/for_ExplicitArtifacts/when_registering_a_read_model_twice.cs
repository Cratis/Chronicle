// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifacts;

public class when_registering_a_read_model_twice : Specification
{
    ExplicitArtifacts _artifacts;

    void Establish() => _artifacts = new ExplicitArtifacts();

    void Because() => _artifacts
        .RegisterProjection<Inventory>(_ => { }, "first")
        .RegisterProjection<Inventory>(_ => { }, "second");

    [Fact] void should_keep_one_registration_for_the_read_model() => _artifacts.Projections.Count().ShouldEqual(1);
    [Fact] void should_keep_the_latest() => _artifacts.Projections.Single().Id.ShouldEqual((ProjectionId)"second");

    public record Inventory(string Name);
}
