// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_exclusion_is_not_proven : given.an_initializer
{
    void Establish() => _proof.IsProven(_name).Returns(false);

    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] async Task should_keep_durable_refusal() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.ExclusionNotProven);
    [Fact] async Task should_have_no_receipts() => (await _audit.GetReceipts()).ShouldBeEmpty();
    [Fact] void should_not_adopt() => _catalog.Records.ShouldBeEmpty();
}
