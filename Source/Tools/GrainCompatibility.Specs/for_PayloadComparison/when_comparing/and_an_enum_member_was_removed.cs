// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_an_enum_member_was_removed : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("enum Cratis.Status : int\n  Active = 1\n  Paused = 2"), PayloadContractManifest.Parse("enum Cratis.Status : int\n  Active = 1"));

    [Fact] void should_report_only_the_removed_member() => _breaks.Select(_ => _.Kind).ShouldEqual([ContractBreakKind.EnumMemberRemoved]);
}
