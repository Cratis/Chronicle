// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_ConnectCompatibility.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

/// <summary>
/// This is #4058: a client built after the kernel gains a field the kernel has never declared. Additions are
/// never breaking - the older side (here, the kernel) simply does not use them - but only once the direction
/// of the comparison is told which side that actually is. Before this fix, <c language="csharp">ConnectionService</c>
/// always treated the connecting client as the older side, so this exact shape was reported as the kernel
/// having a field removed and refused the connection.
/// </summary>
public class a_newer_client_against_an_older_kernel : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: GrownContract.WithAddedField(),
        clientProtocolVersion: "18.4.1",
        kernelContract: GrownContract.Base(),
        kernelProtocolVersion: "18.1.4");

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Incompatibilities.ShouldBeEmpty();
}
