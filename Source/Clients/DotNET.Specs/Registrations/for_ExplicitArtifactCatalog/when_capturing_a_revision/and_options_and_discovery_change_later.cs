// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_capturing_a_revision;

public class and_options_and_discovery_change_later : given.a_catalog
{
    ExplicitArtifactsSnapshot _snapshot;
    List<Type> _nativeEvents;
    IClientArtifactsProvider _nextRevision;

    void Establish()
    {
        _nativeEvents = [typeof(PlainEvent)];
        _discovered.EventTypes.Returns(_nativeEvents);
        _options.RegisterEventType<OtherEvent>(_ => _.WithId("captured"));
        _snapshot = _options.CreateSnapshot();
        _catalog = _snapshot.ApplyTo(_discovered);
    }

    void Because()
    {
        _options.RegisterEventType<PlainArtifact>();
        _nativeEvents.Add(typeof(PlainArtifact));
        _nextRevision = _snapshot.ApplyTo(_discovered);
    }

    [Fact] void should_keep_the_original_discovery_revision_immutable() => _catalog.EventTypes.ShouldContainOnly(typeof(PlainEvent), typeof(OtherEvent));
    [Fact] void should_recapture_native_discovery_on_explicit_rediscovery() => _nextRevision.EventTypes.ShouldContainOnly(typeof(PlainEvent), typeof(OtherEvent), typeof(PlainArtifact));
    [Fact] void should_not_recapture_new_options_recipes() => _snapshot.EventTypes.Select(_ => _.Type).ShouldContainOnly(typeof(OtherEvent));
    [Fact] void should_keep_the_captured_identity() => ExplicitArtifactCatalog.From(_nextRevision)!.GetEventDescriptorFor(typeof(OtherEvent), _store).EventType.Id.Value.ShouldEqual("captured");
}
