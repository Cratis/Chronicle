// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

internal sealed class ProtocolOutput
{
    internal const int Capacity = 256;
    readonly object _gate = new();
    readonly List<string> _lines = [];
    TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completed;
    Exception? _failure;

    public void Add(string line)
    {
        lock (_gate)
        {
            if (_completed) throw new InvalidOperationException("Child stdout is already complete.", _failure);
            if (_lines.Count == Capacity)
            {
                _failure = new InvalidOperationException($"Child stdout exceeded {Capacity} buffered lines.");
                _completed = true;
                Signal();
                throw _failure;
            }
            _lines.Add(line);
            Signal();
        }
    }

    public void Complete(Exception? failure = null)
    {
        lock (_gate)
        {
            _failure ??= failure;
            _completed = true;
            Signal();
        }
    }

    public async Task<string?> WaitForLine(string prefix, CancellationToken cancellationToken)
    {
        while (true)
        {
            Task changed;
            lock (_gate)
            {
                if (_failure is not null) throw new InvalidOperationException("Child stdout failed.", _failure);
                var index = _lines.FindIndex(line => line.StartsWith(prefix + " ", StringComparison.Ordinal));
                if (index >= 0)
                {
                    var line = _lines[index];
                    _lines.RemoveAt(index);
                    return line;
                }
                if (_completed) return null;
                changed = _changed.Task;
            }
            await changed.WaitAsync(cancellationToken);
        }
    }

    void Signal()
    {
        _changed.TrySetResult();
        _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
