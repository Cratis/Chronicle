// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

/// <summary>
/// Ordering by protocol version fixes the false positive in #4058, but it must not turn into never refusing
/// a newer client. A field the older side (here, the kernel) genuinely still needs is still reported missing
/// when it is the client, being newer, that no longer declares it.
/// </summary>
public class a_newer_client_missing_something_the_older_kernel_needs : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: WireContracts.With(field: WireContracts.DefaultField with { Number = 7 }),
        clientProtocolVersion: "18.4.1",
        kernelContract: WireContracts.With(),
        kernelProtocolVersion: "18.1.4");

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_report_the_field_the_client_no_longer_has() =>
        _result.Incompatibilities.ShouldContain(_ =>
            _.Kind == WireIncompatibilityKind.FieldRemoved && _.Description.Contains("Field number 1", StringComparison.Ordinal));
}
