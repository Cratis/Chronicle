// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleOptions.when_creating_for_a_short_lived_process;

public class with_event_types_only_registration : Specification
{
    ChronicleOptions _options;

    void Because() => _options = ChronicleOptions.ForShortLivedProcess("chronicle://short-lived:9090", RegistrationMode.EventTypesOnly);

    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_register_event_types_only() => _options.RegistrationMode.ShouldEqual(RegistrationMode.EventTypesOnly);
}
