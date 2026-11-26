// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public class ReducerFrames
{
    readonly Lock _sync = new();
    readonly List<(ReadModelChangeType Type, EventContext Context)> _frames = [];
    TaskCompletionSource _changed = NewSignal();

    public (ReadModelChangeType Type, EventContext Context)[] For(string key)
    {
        lock (_sync) return _frames.Where(_ => _.Context.EventSourceId.Value == key).ToArray();
    }

    public void Record(ReadModelChangeType type, EventContext context)
    {
        lock (_sync)
        {
            _frames.Add((type, context));
            _changed.TrySetResult();
            _changed = NewSignal();
        }
    }

    public async Task WaitFor(string key, ReadModelChangeType type, EventSequenceNumber sequence, CancellationToken cancellationToken)
    {
        while (true)
        {
            Task changed;
            lock (_sync)
            {
                if (_frames.Exists(_ => _.Context.EventSourceId.Value == key && _.Type == type && _.Context.SequenceNumber == sequence)) return;
                changed = _changed.Task;
            }
            await changed.WaitAsync(cancellationToken);
        }
    }

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
