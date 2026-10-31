// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.when_a_grain_call_fails;

public class and_the_interface_is_external : given.a_call_filter
{
    void Establish() => _context.InterfaceMethod.Returns(typeof(ThirdParty.IExternalPayload<string>).GetMethod(nameof(ThirdParty.IExternalPayload<string>.SecretMethod)));
    Task Because() => Invoke();
    [Fact] void should_replace_both_names_with_external() => _messages.Single().ShouldContain("external.external");
    [Fact] void should_not_disclose_the_type_name() => _messages.Single().ShouldNotContain("IExternalPayload");
    [Fact] void should_not_disclose_the_method_name() => _messages.Single().ShouldNotContain("SecretMethod");
}
