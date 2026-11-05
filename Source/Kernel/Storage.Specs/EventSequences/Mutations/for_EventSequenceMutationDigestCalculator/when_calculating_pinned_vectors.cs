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
    [Fact] void should_match_the_terminal_receipt_vector() => Convert.ToHexStringLower(_calculatedReceiptDigest.Snapshot()).ShouldEqual("8160d2e4bb8dfb1f62889b134d2fb31d8c2eb63b1eae3bff9720bc8f29dc51b9");
}
