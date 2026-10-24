// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

public partial class ProjectionPipeline
{
    bool _isReplaying;

    /// <inheritdoc/>
    public async Task PreparePage(IReadOnlyList<AppendedEvent> events)
    {
        ClearPreparedPage();
        if (!_isReplaying || replayScopedCache is not IEventSequenceStorage storage) return;
        (replayScopedCache as IProjectionPageCache)?.BeginPreparedPage();
        try
        {
            await new ProjectionPagePreparation(storage, sink).Prepare(projection, events);
        }
        catch (Exception exception)
        {
            logger.PagePreparationFailed(exception);

            // Preparation never determines routing or writes. Discard partial reads and use ordinary handling.
            ClearPreparedPage();
        }
    }

    /// <inheritdoc/>
    public void ClearPreparedPage()
    {
        (sink as ISinkPagePrefetch)?.ClearPrefetchPage();
        (replayScopedCache as IProjectionPageCache)?.ClearPreparedPage();
    }
}
