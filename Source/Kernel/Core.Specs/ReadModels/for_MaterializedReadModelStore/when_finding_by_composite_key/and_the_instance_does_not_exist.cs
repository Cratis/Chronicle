// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_finding_by_composite_key;

public class and_the_instance_does_not_exist : given.a_composite_key_store
{
    void Establish() => _lookup.FindByCompositeKey(_key).Returns(CompositeKeyLookupResult.NotFound);
    Task Because() => Find();
    [Fact] void should_not_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
    [Fact] Task should_not_release_compliance() => DidNotRelease();
}
