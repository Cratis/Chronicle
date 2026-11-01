// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_incoming_version_is_lower : given.a_versioned_owner
{
    void Establish() => _incoming = _incoming with { Version = "0.9.0" };
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_keep_the_owner() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.OwnerIsNewer);
    [Fact] void should_identify_the_owning_sequence() => _result.OwnerSequence.ShouldEqual(_owning.ObserverKey.EventSequenceId);
    [Fact] void should_identify_the_owning_version() => _result.OwnerVersion.ShouldEqual("1.0.0");
}
