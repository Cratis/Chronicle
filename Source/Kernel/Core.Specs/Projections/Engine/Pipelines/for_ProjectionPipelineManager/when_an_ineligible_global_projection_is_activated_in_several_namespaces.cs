// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_an_ineligible_global_projection_is_activated_in_several_namespaces : given.a_pipeline_manager
{
    RecordingLoggerFactory _loggers;
    ProjectionPipelineManager _recordingManager;

    async Task Establish()
    {
        _loggers = new();
        _projection.Scope.Returns(ProjectionScope.Global);
        _projection.FencedGlobalIneligibilityReason.Returns("it has children");
        _storage.GetEventStore(_store).GetNamespace(Arg.Any<EventStoreNamespaceName>()).Sinks.GetFor(_projection.ReadModel).Returns(_sink);
        _recordingManager = new(
            _storage,
            _grainFactory,
            Substitute.For<IObjectComparer>(),
            Substitute.For<ITypeFormats>(),
            Substitute.For<IReadModelsCompliance>(),
            Options.Create(new ChronicleOptions()),
            _loggers,
            null,
            _time);
        await Task.CompletedTask;
    }

    async Task Because()
    {
        await _recordingManager.GetFor(_store, "first", _projection);
        await _recordingManager.GetFor(_store, "second", _projection);
        await _recordingManager.GetFor(_store, "third", _projection);
    }

    [Fact] void should_warn_once() => _loggers.Warnings.Count.ShouldEqual(1);
    [Fact] void should_name_the_projection() => _loggers.Warnings[0].ShouldContain("projection");
    [Fact] void should_give_the_reason() => _loggers.Warnings[0].ShouldContain("it has children");

    sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public List<string> Warnings { get; } = [];

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(Warnings);

        public void Dispose()
        {
        }
    }

    sealed class RecordingLogger(List<string> warnings) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                lock (warnings)
                {
                    warnings.Add(formatter(state, exception));
                }
            }
        }
    }
}
