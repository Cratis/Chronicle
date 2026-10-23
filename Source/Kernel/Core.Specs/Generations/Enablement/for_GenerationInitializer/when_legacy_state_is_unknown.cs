// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_legacy_state_is_unknown : given.an_initializer
{
    void Establish() => _models.GetAll(_name).Returns(_ => Task.FromException<IReadOnlyList<LegacyMaterialization>>(new InvalidOperationException("Missing observer state.")));

    async Task Because() => _result = await _initializer.Initialize(_name);

    [Fact] async Task should_keep_a_durable_unknown_blocker() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.UnknownLegacyState);
    [Fact] void should_not_adopt() => _catalog.Records.ShouldBeEmpty();
}
