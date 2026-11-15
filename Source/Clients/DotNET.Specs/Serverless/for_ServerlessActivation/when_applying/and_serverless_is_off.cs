// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessActivation.when_applying;

public class and_serverless_is_off : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    void Because() => ServerlessActivation.Apply(_options, "Production");

    [Fact] void should_not_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeFalse();
    [Fact] void should_register_inline() => _options.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
}
