// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor;

/// <summary>
/// Startup subscribes pattern capture for every namespace that exists and event type registration re-subscribes
/// when the type list grows - but a namespace added while the server runs, with no type change to piggyback on,
/// would otherwise mine nothing until the next restart.
/// </summary>
public class when_namespace_is_added : Specification
{
    NamespacesReactor _reactor;
    IPatternCapture _patternCapture;

    void Establish()
    {
        _patternCapture = Substitute.For<IPatternCapture>();

        var seeding = Substitute.For<IResultAwareEventSeeding>();
        seeding.GetSeededEvents().Returns(new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>()));

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IResultAwareEventSeeding>(Arg.Any<string>(), Arg.Any<string>()).Returns(seeding);

        _reactor = new(grainFactory, _patternCapture);
    }

    async Task Because() => await _reactor.Added(new NamespaceAdded("some-store", "some-namespace"), null!);

    [Fact] async Task should_subscribe_pattern_capture_for_the_namespace() =>
        await _patternCapture.Received(1).Subscribe("some-store", "some-namespace");
}
