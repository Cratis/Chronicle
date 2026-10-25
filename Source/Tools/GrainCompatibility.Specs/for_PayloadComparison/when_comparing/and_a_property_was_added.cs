// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_a_property_was_added : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("json Cratis.Payload\n  name: string"), PayloadContractManifest.Parse("json Cratis.Payload\n  name: string\n  age: int"));

    [Fact] void should_keep_serving_the_previous_release() => _breaks.ShouldBeEmpty();
}
