// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding;

public class and_the_old_observer_is_external : given.a_renamed_reactor
{
    void Establish()
    {
        _stored[0].Type = ObserverType.External;
    }

    void Because() => _result = ObserverIdentityDrift.Find(_stored, _current).ToArray();

    [Fact] void should_report_only_likely_identity_changes() => _result.ShouldBeEmpty();
}
