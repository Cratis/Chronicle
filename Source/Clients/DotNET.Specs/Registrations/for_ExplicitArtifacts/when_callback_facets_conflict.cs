// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifacts;

public class when_callback_facets_conflict : Specification
{
    ExplicitArtifacts _options;
    Exception _error;

    void Establish() => _options = new();

    void Because() => _error = Catch.Exception(() => _options.RegisterEventType<Event>(_ => _.Public().Private()));

    [Fact] void should_reject_the_incompatible_supplied_facet() => _error.ShouldBeOfExactType<ConflictingArtifactDeclaration>();
    [Fact] void should_not_capture_a_partial_recipe() => _options.CreateSnapshot().EventTypes.ShouldBeEmpty();

    public record Event;
}
