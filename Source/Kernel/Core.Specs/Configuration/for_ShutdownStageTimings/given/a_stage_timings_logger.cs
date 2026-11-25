// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Configuration.for_ShutdownStageTimings.given;

public class a_stage_timings_logger : Specification
{
    protected readonly CancellationTokenSource _deadline = new();
    protected readonly List<(LogLevel Level, string Message)> _logged = [];
    protected ManualTimeProvider _time;
    protected ISiloLifecycle _lifecycle;
    protected List<ILifecycleObserver> _observers;
    protected List<int> _stages;
    protected ShutdownStageTimings _timings;

    void Establish()
    {
        _time = new();
        var budget = Substitute.For<IShutdownDrainBudget>();
        budget.Token.Returns(_deadline.Token);
        budget.Timeout.Returns(TimeSpan.FromSeconds(8));
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(new CancellationTokenSource().Token);
        var logger = new CapturingLogger(_logged);

        _observers = [];
        _stages = [];
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>()).Returns(call =>
        {
            _stages.Add(call.ArgAt<int>(1));
            _observers.Add(call.ArgAt<ILifecycleObserver>(2));
            return Substitute.For<IDisposable>();
        });

        _timings = new ShutdownStageTimings(lifetime, budget, logger, _time);
        _timings.Participate(_lifecycle);
    }

    void Destroy()
    {
        _timings.Dispose();
        _deadline.Dispose();
    }

    protected async Task StopStagesInDescendingOrder(TimeSpan perStage)
    {
        foreach (var index in Enumerable.Range(0, _stages.Count).OrderByDescending(_ => _stages[_]))
        {
            await _observers[index].OnStop(CancellationToken.None);
            _time.Advance(perStage);
        }
    }

    protected sealed class ManualTimeProvider : TimeProvider
    {
        long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan by) => _timestamp += by.Ticks;
    }

    sealed class CapturingLogger(List<(LogLevel Level, string Message)> logged) : ILogger<ShutdownStageTimings>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logged.Add((logLevel, formatter(state, exception)));
    }
}
