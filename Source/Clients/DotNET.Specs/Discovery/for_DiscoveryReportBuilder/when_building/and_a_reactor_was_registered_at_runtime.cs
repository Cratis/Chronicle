// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_DiscoveryReportBuilder.when_building;

public class and_a_reactor_was_registered_at_runtime : given.a_report_builder
{
    DiscoveryReport _before;

    async Task Establish()
    {
        _before = _builder.Build();
        await _reactors.Register("runtime", builder => builder.WithEventType(_eventType), (_, _) => Task.CompletedTask);
    }

    void Because() => _report = _builder.Build();

    [Fact] void should_include_the_new_reactor_in_a_fresh_report() => _report.OfKind(ArtifactKind.Reactor).ShouldContainOnly(new DiscoveredArtifact(ArtifactKind.Reactor, "runtime", null));
    [Fact] void should_not_change_the_published_snapshot() => _before.CountOf(ArtifactKind.Reactor).ShouldEqual(0);
}
