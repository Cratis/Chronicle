// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_nothing_changed;

public class and_the_contracts_are_identical : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = WireCompatibilityChecker.Check(WireContracts.With(), WireContracts.With());

    [Fact] void should_report_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Incompatibilities.ShouldBeEmpty();
}
