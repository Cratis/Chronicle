// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.given;

public class application_metadata_sharing : schema_sharing
{
    protected readonly InMemoryClientArtifactsProvider _boundArtifacts = new()
    {
        EventTypes = [typeof(StoreIdentityRecorded)],
        ModelBoundProjections = [typeof(BoundReadModel)]
    };

    protected EventStoreForTesting _third;
    bool _previousSharingOfMetadata;

    void Establish()
    {
        _previousSharingOfMetadata = EventStoreForTesting.ShareApplicationMetadata;
        EventStoreForTesting.ShareApplicationMetadata = true;
    }

    void Destroy()
    {
        EventStoreForTesting.ShareApplicationMetadata = _previousSharingOfMetadata;
        _third?.Dispose();
    }

    [FromEvent<StoreIdentityRecorded>]
    public record BoundReadModel([Key] string Id, string Value);
}
