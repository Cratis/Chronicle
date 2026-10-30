// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_ConnectCompatibility.when_checking;

public class an_unstamped_client_with_incompatible_contracts : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = ConnectCompatibility.Check(
        clientContract: WireContracts.With(field: WireContracts.DefaultField with { Number = 7 }),
        clientProtocolVersion: "1.0.0",
        kernelContract: WireContracts.With(),
        kernelProtocolVersion: "19.109.0");

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();
    [Fact] void should_preserve_the_client_as_older_report() => _result.Incompatibilities.Single().ShouldMatch(_ => _.Kind == WireIncompatibilityKind.FieldRemoved && _.Description.Contains("Field number 7", StringComparison.Ordinal));
}
