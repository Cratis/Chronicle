// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_confirming_a_write;

public class and_a_newer_write_is_not_confirmed : given.a_recorded_transition
{
    Exception? _error;

    void Establish() => _current = AlertIncidentFold.Apply(null, _raise).Incident;
    void Because() => _error = Catch.Exception(() => AlertIncidentStorageRules.Confirm(_current, _raise with { SequenceNumber = 11UL }));

    [Fact] void should_fail_instead_of_acknowledging_a_lower_diagnostic_position() => _error.ShouldBeOfExactType<AlertIncidentWriteNotConfirmed>();
}
