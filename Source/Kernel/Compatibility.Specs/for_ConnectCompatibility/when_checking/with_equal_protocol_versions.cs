// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

/// <summary>
/// A tie resolves to the direction Chronicle has always checked - does the kernel still serve the client -
/// rather than either an arbitrary or the reversed one. Two peers built from the same release are expected
/// to carry the same contract, so this only matters for a pathological same-version-different-contract case,
/// but the choice still has to be deliberate rather than accidental.
/// </summary>
public class with_equal_protocol_versions : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: WireContracts.With(),
        clientProtocolVersion: "18.1.4",
        kernelContract: WireContracts.With(method: WireContracts.DefaultMethod with { Name = "DoSomethingElse" }),
        kernelProtocolVersion: "18.1.4");

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_report_the_method_the_kernel_no_longer_has() =>
        _result.Incompatibilities.ShouldContain(_ => _.Kind == WireIncompatibilityKind.MethodRemoved);
}
