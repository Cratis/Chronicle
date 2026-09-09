// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_encoding_a_guid : given.a_digest_calculation
{
    string _digest;

    void Because() => _digest = Convert.ToHexStringLower(CalculateDefinition().Snapshot());

    [Fact] void should_use_rfc_network_byte_order() => _digest.ShouldEqual("58132b763a718b5074b1c8f98b113e8740e03a2077f89eb6fe1b8a67ec242928");
    [Fact] void should_not_use_dotnet_mixed_endian_byte_order() => _digest.ShouldNotEqual("85b2877f816f32ff5501c5a4ad9319a12d42f60bec72041e12af5cd41a85df9c");
}
