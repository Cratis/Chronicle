// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_ConnectCompatibility.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

/// <summary>
/// The ordinary, by far the most common case: a kernel is upgraded and gains a field, an older client that
/// has never heard of it connects. This has always worked and must keep working exactly as before.
/// </summary>
public class an_older_client_against_a_newer_kernel : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: GrownContract.Base(),
        clientProtocolVersion: "18.1.4",
        kernelContract: GrownContract.WithAddedField(),
        kernelProtocolVersion: "18.4.1");

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Incompatibilities.ShouldBeEmpty();
}
