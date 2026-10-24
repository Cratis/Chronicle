// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key;

public class and_another_key_owns_the_legacy_id : given.a_composite_key_store
{
    void Establish() => _lookup.FindByCompositeKey(_key).Returns(CompositeKeyLookupResult.KeyCollision);
    Task Because() => Find();
    [Fact] void should_preserve_the_outcome() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.KeyCollision);
    [Fact] Task should_not_release_compliance() => DidNotRelease();
}
