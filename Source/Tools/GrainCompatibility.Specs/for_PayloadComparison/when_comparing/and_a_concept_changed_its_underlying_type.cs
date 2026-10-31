// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_a_concept_changed_its_underlying_type : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("concept Cratis.Id : string"), PayloadContractManifest.Parse("concept Cratis.Id : int"));

    [Fact] void should_report_only_the_changed_representation() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.PayloadKindChanged]);
}
