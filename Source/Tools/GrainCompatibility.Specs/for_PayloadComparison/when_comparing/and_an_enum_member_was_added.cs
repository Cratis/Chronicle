// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_an_enum_member_was_added : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("enum Cratis.Status : int\n  Active = 1"), PayloadContractManifest.Parse("enum Cratis.Status : int\n  Active = 1\n  Paused = 2"));

    [Fact] void should_keep_serving_the_previous_release() => _breaks.ShouldBeEmpty();
}
