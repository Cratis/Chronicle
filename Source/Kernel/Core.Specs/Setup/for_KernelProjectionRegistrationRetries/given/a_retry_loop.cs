// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Kernel;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.for_KernelProjectionRegistrationRetries.given;

public class a_retry_loop : Specification
{
    protected IKernelProjections _kernelProjections;
    protected KernelProjectionRegistrationRetries _retries;
    protected EventStoreName _eventStore = "event-store";
    protected manual_time_provider _timeProvider;
    protected retry_logger _logger;
    protected double _jitter = 1;

    void Establish()
    {
        _kernelProjections = Substitute.For<IKernelProjections>();
        _timeProvider = new();
        _logger = new();
        _retries = new(_kernelProjections, _logger, _timeProvider, () => _jitter);
    }

    Task Destroy() => _retries.DisposeAsync().AsTask();

    protected sealed class retry_logger : ILogger<KernelProjectionRegistrationRetries>
    {
        readonly Channel<LogLevel> _levels = Channel.CreateUnbounded<LogLevel>();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _levels.Writer.TryWrite(logLevel);
        public Task<LogLevel> Next() => _levels.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    protected sealed class manual_time_provider : TimeProvider
    {
        readonly Channel<manual_timer> _timers = Channel.CreateUnbounded<manual_timer>();
        public List<TimeSpan> Delays { get; } = [];
        public Task<manual_timer> Next() => _timers.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new manual_timer(callback, state);
            Delays.Add(dueTime);
            _timers.Writer.TryWrite(timer);
            return timer;
        }
    }

    protected sealed class manual_timer(TimerCallback callback, object? state) : ITimer
    {
        bool _disposed;
        public void Complete()
        {
            if (!_disposed) callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() => _disposed = true;
        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
