// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Clients;

public class when_the_debugger_exemption_is_disallowed : Specification
{
    Clients _options;

    void Establish() => _options = new() { AllowDebuggerExemption = false };

    [Fact] void should_not_exempt_a_debugger() => _options.ExemptsDebugger(true).ShouldBeFalse();
}
