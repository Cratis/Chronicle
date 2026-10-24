// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_ConnectCompatibility.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

public class an_unstamped_client_with_an_added_field : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: GrownContract.WithAddedField(),
        clientProtocolVersion: "1.0.0",
        kernelContract: GrownContract.Base(),
        kernelProtocolVersion: "19.109.0");

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
}
