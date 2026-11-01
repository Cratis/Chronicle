// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_removal_arrives_during_the_first_scan : given.a_paused_first_lookup
{
    Key? _after;

    async Task Because()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var removal = Task.Run(async () =>
        {
            started.TrySetResult();
            await Remove(_first);
        });
        try
        {
            await started.Task.WaitAsync(Deadline);
        }
        finally
        {
            _releaseScan.TrySetResult();
        }

        await Task.WhenAll(_lookup, removal).WaitAsync(Deadline);
        _after = await Find("old");
    }

    [Fact] void should_not_resurrect_the_removed_root() => _after.ShouldBeNull();
}
