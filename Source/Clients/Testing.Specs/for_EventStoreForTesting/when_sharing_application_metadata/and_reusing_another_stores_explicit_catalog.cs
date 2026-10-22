// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_reusing_another_stores_explicit_catalog : given.application_metadata_sharing
{
    EventStoreForTesting _explicit;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<ExplicitlyRegistered>(_ => _.WithId("explicit-identity"));
        _explicit = new(null, new InMemoryClientArtifactsProvider(), options);
    }

    void Because()
    {
        _other = new(null, _explicit.ClientArtifactsProvider);
        _third = new(null, _explicit.ClientArtifactsProvider);
    }

    void Destroy() => _explicit?.Dispose();

    [Fact] void should_not_share_metadata_for_the_first_reuse() => _other.Metadata.ShouldBeNull();
    [Fact] void should_not_share_metadata_for_the_second_reuse() => _third.Metadata.ShouldBeNull();
    [Fact] void should_keep_the_explicit_event_type_identity() => _other.EventTypes.GetEventTypeFor(typeof(ExplicitlyRegistered)).Id.Value.ShouldEqual("explicit-identity");

    public record ExplicitlyRegistered(string Value);
}
