// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_two_old_observers_share_the_name : given.a_renamed_reactor
{
    void Establish()
    {
        _stored = [.. _stored, new() { Id = "Other.SendEmail", Type = ObserverType.Reactor, Owner = ObserverOwner.None }];
    }

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_only_likely_identity_changes() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Original.SendEmail", "Renamed.SendEmail"), new ArtifactIdentityDrift(ArtifactKind.Reactor, "Other.SendEmail", "Renamed.SendEmail"));
}
