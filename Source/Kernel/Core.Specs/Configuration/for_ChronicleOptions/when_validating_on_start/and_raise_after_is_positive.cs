// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_on_start;

public class and_raise_after_is_positive : given.configured_alert_options
{
    Exception _error;

    void Establish() => Configure("00:05:00");

    void Because() => _error = Catch.Exception(_startupValidator.Validate);

    [Fact] void should_allow_a_grace_period() => _error.ShouldBeNull();
}
