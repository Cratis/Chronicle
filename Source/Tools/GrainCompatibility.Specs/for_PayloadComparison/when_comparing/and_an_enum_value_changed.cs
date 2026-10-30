// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_an_enum_value_changed : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("enum Cratis.Status : int\n  Active = 1"), PayloadContractManifest.Parse("enum Cratis.Status : int\n  Active = 2"));

    [Fact] void should_report_only_the_changed_value() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.EnumValueChanged]);
}
