// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries.given;

public class a_retry_budget : Specification
{
    protected RetryClock _clock;
    protected NamespaceCommitContext _context;
    protected NamespaceCommitRetries _retries;
    protected CancellationTokenSource _activation;
    protected List<TimeSpan> _delays;
    protected Func<TimeSpan, CancellationToken, Task> _delay;
    protected int _jitter;
    protected Exception _error;

    void Establish()
    {
        _clock = new();
        _activation = new();
        _context = new(NamespaceIncarnationId.New(), _activation.Token);
        _delays = [];
        _jitter = 25;
        _delay = (_, _) => Task.CompletedTask;
        _retries = new(
            _clock,
            (duration, token) =>
            {
                _delays.Add(duration);
                return _delay(duration, token);
            },
            () => _jitter);
    }

    void Destroy() => _activation.Dispose();
}
