// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter.given;

public class a_reporter : Specification
{
    private protected static readonly UnrecoverableProtectedValueReporter.Identity Identity = new("store", "namespace", "subject", "value", "Release");
    protected ILogger _logger;
    protected UnrecoverableProtectedValueReporter _reporter;
    protected EncryptionKeyUnrecoverable _failure;
    protected clock _clock;
    protected int _count;

    void Establish()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _clock = new();
        _failure = new(3, new InvalidOperationException("sensitive inner details"));
        _reporter = new(_logger, _clock, () => Interlocked.Increment(ref _count));
    }

    protected class clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
