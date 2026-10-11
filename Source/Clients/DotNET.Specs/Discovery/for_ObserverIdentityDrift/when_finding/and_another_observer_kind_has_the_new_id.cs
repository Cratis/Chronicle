// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_another_observer_kind_has_the_new_id : given.a_renamed_reactor
{
    void Establish() => _stored = [.. _stored, new() { Id = "Renamed.SendEmail", Type = ObserverType.Reducer, Owner = ObserverOwner.Client }];

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_the_reactor_rename() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Original.SendEmail", "Renamed.SendEmail"));
}
