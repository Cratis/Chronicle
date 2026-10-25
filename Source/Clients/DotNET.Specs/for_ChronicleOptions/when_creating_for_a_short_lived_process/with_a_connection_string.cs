// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleOptions.when_creating_for_a_short_lived_process;

public class with_a_connection_string : Specification
{
    ChronicleOptions _options;

    void Because() => _options = ChronicleOptions.ForShortLivedProcess("chronicle://short-lived:9090");

    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_register_inline() => _options.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
    [Fact] void should_not_skip_compatibility_check() => _options.SkipCompatibilityCheck.ShouldBeFalse();
    [Fact] void should_keep_auto_discover_and_register() => _options.AutoDiscoverAndRegister.ShouldBeTrue();
    [Fact] void should_parse_the_connection_string() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("short-lived");
    [Fact] void should_set_authentication() => _options.Authentication.ShouldNotBeNull();
}
