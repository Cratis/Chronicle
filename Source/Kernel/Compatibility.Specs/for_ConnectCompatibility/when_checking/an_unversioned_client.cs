// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_ConnectCompatibility.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

/// <summary>
/// A client too old to send a protocol version at all reads as the oldest possible version, so it falls back
/// to the direction Chronicle has always checked rather than being mistaken for the newer side.
/// </summary>
public class an_unversioned_client : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: GrownContract.Base(),
        clientProtocolVersion: string.Empty,
        kernelContract: GrownContract.WithAddedField(),
        kernelProtocolVersion: "18.4.1");

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
}
