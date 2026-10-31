// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_a_reactor_moved_namespace : given.a_renamed_reactor
{
    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_only_likely_identity_changes() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Original.SendEmail", "Renamed.SendEmail"));
}
