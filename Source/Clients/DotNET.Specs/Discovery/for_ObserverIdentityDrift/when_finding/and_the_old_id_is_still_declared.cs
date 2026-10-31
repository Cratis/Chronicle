// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_the_old_id_is_still_declared : given.a_renamed_reactor
{
    void Establish()
    {
        _current = _current with { Artifacts = _current.Artifacts.Add(new(ArtifactKind.Reactor, "Original.SendEmail", null)) };
    }

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_only_likely_identity_changes() => _result.ShouldBeEmpty();
}
