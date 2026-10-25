// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleOptions.when_using_short_lived_process;

public class on_options_that_already_exist : Specification
{
    ChronicleOptions _options;
    ChronicleOptions _result;

    void Establish() => _options = new ChronicleOptions { SkipCompatibilityCheck = true };

    void Because() => _result = _options.UseShortLivedProcess(RegistrationMode.AtDeployment);

    [Fact] void should_return_the_same_instance() => _result.ShouldBeSame(_options);
    [Fact] void should_keep_prior_settings() => _options.SkipCompatibilityCheck.ShouldBeTrue();
    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_set_the_registration_mode() => _options.RegistrationMode.ShouldEqual(RegistrationMode.AtDeployment);
}
