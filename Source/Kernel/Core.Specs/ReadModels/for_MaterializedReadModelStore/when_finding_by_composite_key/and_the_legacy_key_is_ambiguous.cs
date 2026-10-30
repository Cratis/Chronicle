// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key;

public class and_the_legacy_key_is_ambiguous : given.a_composite_key_store
{
    void Establish() => _lookup.FindByCompositeKey(_key).Returns(CompositeKeyLookupResult.AmbiguousLegacyKey);
    Task Because() => Find();
    [Fact] void should_preserve_the_outcome() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
    [Fact] Task should_not_release_compliance() => DidNotRelease();
}
