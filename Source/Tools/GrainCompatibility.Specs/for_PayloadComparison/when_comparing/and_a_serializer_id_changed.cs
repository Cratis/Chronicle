// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_a_serializer_id_changed : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("orleans Cratis.Payload\n  [Id(0)] Name: string"), PayloadContractManifest.Parse("orleans Cratis.Payload\n  [Id(1)] Name: string"));

    [Fact] void should_report_only_the_removed_serializer_id() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.PayloadPropertyRemoved]);
}
