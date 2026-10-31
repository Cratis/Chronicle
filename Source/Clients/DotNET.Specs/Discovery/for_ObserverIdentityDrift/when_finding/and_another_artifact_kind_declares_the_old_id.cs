// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_another_artifact_kind_declares_the_old_id : given.a_renamed_reactor
{
    void Establish() => _current = _current with { Artifacts = _current.Artifacts.Add(new(ArtifactKind.EventType, "Original.SendEmail", null)) };

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_the_reactor_rename() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Original.SendEmail", "Renamed.SendEmail"));
}
