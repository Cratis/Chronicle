// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleOptions;

public class when_constructing_with_defaults : Specification
{
    ChronicleOptions _options;

    void Because() => _options = new ChronicleOptions();

    [Fact] void should_not_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeFalse();
    [Fact] void should_register_inline() => _options.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
}
