// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.given;

public class UncooperativeConstraintValues(CancellationTokenSource deadline) : IAsyncEnumerable<UniqueConstraintValue>, IAsyncEnumerator<UniqueConstraintValue>
{
    readonly TaskCompletionSource<bool> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public UniqueConstraintValue Current => new("unused");
    public IAsyncEnumerator<UniqueConstraintValue> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;
    public async ValueTask<bool> MoveNextAsync()
    {
        // This store deliberately ignores cancellation; the caller must bound the awaited read.
        await deadline.CancelAsync();
        return await _completed.Task;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public void Complete() => _completed.TrySetResult(false);
}
