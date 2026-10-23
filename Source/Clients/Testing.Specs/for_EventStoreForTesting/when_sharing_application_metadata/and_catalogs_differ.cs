// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_catalogs_differ : given.application_metadata_sharing
{
    void Because() => _other = new(null, _boundArtifacts);

    [Fact] void should_not_share_the_metadata() => _store.Metadata.ShouldNotBeSame(_other.Metadata);
}
