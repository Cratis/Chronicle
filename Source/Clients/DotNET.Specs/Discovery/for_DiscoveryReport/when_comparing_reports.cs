// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Discovery.for_DiscoveryReport;

public class when_comparing_reports : Specification
{
    DiscoveryReport _report;
    DiscoveryReport _equivalent;

    void Establish()
    {
        _report = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.Reactor, "send", typeof(string))));
        _equivalent = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.Reactor, "send", typeof(string))));
    }

    [Fact] void should_compare_by_value() => _report.Equals(_equivalent).ShouldBeTrue();
    [Fact] void should_hash_alike() => _report.GetHashCode().ShouldEqual(_equivalent.GetHashCode());
    [Fact] void should_distinguish_not_run() => _report.Equals(DiscoveryReport.NotRun).ShouldBeFalse();
}
