// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Enablement.for_NotProvenGenerationExclusionProof;

public class when_no_operator_proof_is_installed : Specification
{
    bool _proven;

    async Task Because() => _proven = await new NotProvenGenerationExclusionProof().IsProven(new("orders"));

    [Fact] void should_fail_closed() => _proven.ShouldBeFalse();
}
