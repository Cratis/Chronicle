// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.given;

public class a_continuation_checkpoint : Specification
{
    internal readonly ObserverContinuationLifetime _lifetime = new();
    internal ObserverContinuationCheckpoint _checkpoint;
    protected int _invocations;
    protected bool _continued;
    protected bool _genericContinued;
    protected Exception? _error;
    protected Exception? _genericError;

    void Establish() => _checkpoint = _lifetime.Capture();

    protected async Task ContinueAfter(Func<Task> operation)
    {
        await _checkpoint.Await(operation);
        _continued = true;
    }

    protected async Task ContinueAfterResult(Func<Task<int>> operation)
    {
        await _checkpoint.Await(operation);
        _genericContinued = true;
    }
}
