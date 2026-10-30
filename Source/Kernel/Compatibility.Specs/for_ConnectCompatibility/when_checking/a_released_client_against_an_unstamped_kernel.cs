// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_ConnectCompatibility.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

public class a_released_client_against_an_unstamped_kernel : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: GrownContract.Base(),
        clientProtocolVersion: "19.109.0",
        kernelContract: GrownContract.WithAddedField(),
        kernelProtocolVersion: "1.0.0");

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
}
