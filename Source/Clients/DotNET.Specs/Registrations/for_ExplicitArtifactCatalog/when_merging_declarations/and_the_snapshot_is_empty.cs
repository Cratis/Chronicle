// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_the_snapshot_is_empty : given.a_catalog
{
    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_return_the_original_provider() => _catalog.ShouldBeSame(_discovered);
    [Fact] void should_not_read_the_providers_event_types() => _ = _discovered.DidNotReceive().EventTypes;
}
