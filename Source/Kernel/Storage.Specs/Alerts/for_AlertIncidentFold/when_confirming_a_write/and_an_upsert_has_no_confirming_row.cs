// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_AlertIncidentFold.when_confirming_a_write;

public class and_an_upsert_has_no_confirming_row : given.a_recorded_transition
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => AlertIncidentStorageRules.Confirm(null, _raise));

    [Fact] void should_fail_an_undeterminable_outcome() => _error.ShouldBeOfExactType<AlertIncidentWriteNotConfirmed>();
}
