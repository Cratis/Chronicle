// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_two_types_share_an_identity : given.a_catalog
{
    Exception _error;

    void Establish() => _options
        .RegisterEventType<PlainEvent>(_ => _.WithId("same"))
        .RegisterEventType<OtherEvent>(_ => _.WithId("same"));

    void Because() => _error = Catch.Exception(() => _options.CreateSnapshot().ApplyTo(_discovered));

    [Fact] void should_reject_the_collision() => _error.ShouldBeOfExactType<MultipleEventTypesWithSameIdFound>();
}
