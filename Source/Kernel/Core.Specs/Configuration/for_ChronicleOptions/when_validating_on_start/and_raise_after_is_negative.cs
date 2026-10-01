// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_on_start;

public class and_raise_after_is_negative : given.configured_alert_options
{
    Exception _error;

    void Establish() => Configure("-00:00:01");

    void Because() => _error = Catch.Exception(_startupValidator.Validate);

    [Fact] void should_reject_invalid_options_before_starting() => _error.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_identify_the_condition_and_setting() => ((OptionsValidationException)_error).Failures.Single().Contains("Alerts.Conditions['partition-failing'].RaiseAfter", StringComparison.Ordinal).ShouldBeTrue();
}
