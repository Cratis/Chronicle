// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

public class and_everything_is_gone : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = WireCompatibilityChecker.Check(WireContracts.With(), WireContracts.Empty());

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();
    [Fact] void should_report_the_missing_service() => KindsFor(WireContracts.Service).ShouldContain(WireIncompatibilityKind.ServiceRemoved);
    [Fact] void should_report_the_missing_message() => KindsFor(WireContracts.Message).ShouldContain(WireIncompatibilityKind.MessageRemoved);
    [Fact] void should_report_the_missing_enum() => KindsFor(WireContracts.Enum).ShouldContain(WireIncompatibilityKind.EnumRemoved);

    IEnumerable<WireIncompatibilityKind> KindsFor(string path) =>
        _result.Incompatibilities.Where(_ => _.Path == path).Select(_ => _.Kind);
}
