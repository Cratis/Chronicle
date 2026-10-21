// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_a_reducer_moved_namespace : given.a_renamed_reactor
{
    void Establish()
    {
        _stored[0].Type = ObserverType.Reducer;
        _current = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.Reducer, "Renamed.SendEmail", null)));
    }

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_only_likely_identity_changes() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reducer, "Original.SendEmail", "Renamed.SendEmail"));
}
