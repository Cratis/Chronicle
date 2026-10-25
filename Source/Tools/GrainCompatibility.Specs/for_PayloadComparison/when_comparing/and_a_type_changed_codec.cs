// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_a_type_changed_codec : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("json Cratis.Payload\n  name: string"), PayloadContractManifest.Parse("orleans Cratis.Payload\n  [Id(0)] Name: string"));

    [Fact] void should_report_only_the_changed_codec() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.PayloadKindChanged]);
}
