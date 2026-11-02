// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Clients;

public class when_using_the_defaults : Specification
{
    Clients _options;

    void Because() => _options = new();

    [Fact] void should_allow_the_debugger_exemption() => _options.AllowDebuggerExemption.ShouldBeTrue();
    [Fact] void should_exempt_a_debugger() => _options.ExemptsDebugger(true).ShouldBeTrue();
    [Fact] void should_not_exempt_a_client_without_a_debugger() => _options.ExemptsDebugger(false).ShouldBeFalse();
}
