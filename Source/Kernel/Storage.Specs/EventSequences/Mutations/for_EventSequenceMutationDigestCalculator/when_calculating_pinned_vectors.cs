// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_calculating_pinned_vectors : given.a_digest_calculation
{
    EventSequenceMutationDefinitionDigestV1 _calculatedDefinitionDigest;
    EventSequenceMutationReceiptDigestV1 _calculatedReceiptDigest;

    void Because()
    {
        _calculatedDefinitionDigest = CalculateDefinition();
        _calculatedReceiptDigest = CalculateReceipt(definitionDigest: _calculatedDefinitionDigest);
    }

    [Fact] void should_match_the_definition_vector() => Convert.ToHexStringLower(_calculatedDefinitionDigest.Snapshot()).ShouldEqual("58132b763a718b5074b1c8f98b113e8740e03a2077f89eb6fe1b8a67ec242928");
    [Fact] void should_match_the_terminal_receipt_vector() => Convert.ToHexStringLower(_calculatedReceiptDigest.Snapshot()).ShouldEqual("f7f4673deaad71914379eb2cdc5ed9c069525661c60779ecbfbfb857eb3c84a0");
}
