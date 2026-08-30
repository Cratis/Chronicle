// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

/// <summary>
/// This one has already happened: three rpcs went from server streaming to unary inside 16.x, and nothing said so.
/// A caller expecting a stream does not decode something wrong - it hangs or errors on the call itself.
/// </summary>
public class and_a_method_changed_call_shape : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = WireCompatibilityChecker.Check(
        WireContracts.With(method: WireContracts.DefaultMethod with { ServerStreaming = true }),
        WireContracts.With());

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_report_the_streaming_change() =>
        _result.Incompatibilities.ShouldContain(_ => _.Kind == WireIncompatibilityKind.MethodStreamingChanged);

    [Fact] void should_say_what_it_went_from_and_to() =>
        _result.Incompatibilities.Single(_ => _.Kind == WireIncompatibilityKind.MethodStreamingChanged)
            .Description.ShouldContain("from server streaming to unary");
}
