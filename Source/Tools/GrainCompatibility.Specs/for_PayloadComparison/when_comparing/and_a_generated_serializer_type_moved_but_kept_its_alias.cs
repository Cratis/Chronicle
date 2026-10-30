// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadComparison.when_comparing;

public class and_a_generated_serializer_type_moved_but_kept_its_alias : Specification
{
    IReadOnlyList<ContractBreak> _breaks;

    void Because() => _breaks = PayloadContractComparison.Compare(PayloadContractManifest.Parse("orleans [Alias(\"payload\")] Cratis.Payload\n  [Id(0)] Name: string"), PayloadContractManifest.Parse("orleans [Alias(\"payload\")] Cratis.Moved.Payload\n  [Id(0)] Name: string"));

    [Fact] void should_keep_serving_the_previous_release() => _breaks.ShouldBeEmpty();
}
