// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Pauses the first fenced batch reaching an armed point until released.
/// </summary>
public class PausingProbe : IFencedBatchProbe
{
    readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    FencePausePoint _armed;

    public Task Reached => _reached.Task;

    public void Arm(FencePausePoint point) => _armed = point;

    public void Release() => _released.TrySetResult();

    public Task BeforeAdmission(FenceBatchId batchId) => At(FencePausePoint.BeforeAdmission);

    public Task AfterAdmission(FenceBatchId batchId) => At(FencePausePoint.AfterAdmission);

    public Task MidExecution() => At(FencePausePoint.MidExecution);

    public Task BeforeCommit(FenceBatchId batchId) => At(FencePausePoint.BeforeCommit);

    async Task At(FencePausePoint point)
    {
        if (_armed != point)
        {
            return;
        }

        _armed = FencePausePoint.None;
        _reached.TrySetResult();
        await _released.Task.WaitAsync(TimeSpan.FromSeconds(60));
    }
}
