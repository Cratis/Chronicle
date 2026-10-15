// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_an_explicit_id_replaced_the_type_name : given.a_renamed_reactor
{
    void Establish()
    {
        _current = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.Reactor, "send-email", typeof(SendEmail))));
    }

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_only_likely_identity_changes() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Original.SendEmail", "send-email"));
}
