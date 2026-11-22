// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class held_storage_operation<T>
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<T> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Finished => _finished.Task;

    public async Task<T> Invoke()
    {
        _started.TrySetResult();
        try
        {
            return await _completion.Task;
        }
        finally
        {
            _finished.TrySetResult();
        }
    }

    public Task WaitForStart() => _started.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    public void Complete(T result) => _completion.TrySetResult(result);
    public void Fail(Exception error) => _completion.TrySetException(error);
}
