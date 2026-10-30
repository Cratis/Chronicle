// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.given;

public class a_reactor_dispatcher : all_dependencies
{
    internal ReactorDispatcher _dispatcher;
    protected ReactorHandler _handler;
    protected List<string> _notifications;

    void Establish()
    {
        _notifications = [];
        _dispatcher = new(
            _eventStore,
            _eventTypes,
            _serviceProvider,
            _artifactActivator,
            _middlewaresActivator,
            _eventSerializer,
            _activitySource,
            _sideEffectHandlers,
            _reactorContextValuesBuilder,
            _argumentsResolver,
            _logger,
            LoggerFactory);
        _handler = new(_eventStore, "reactor", typeof(NotifiedReactor), EventSequenceId.Log, [], _causationManager, _identityProvider);
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(NotifiedReactor)).Returns(_ =>
            new ActivatedArtifact(new NotifiedReactor(_notifications), typeof(NotifiedReactor), Substitute.For<ILogger<ActivatedArtifact>>()));
    }

    void Destroy() => _handler.Dispose();

    public class NotifiedReactor(List<string> notifications) : ICanBeNotifiedWhenReplay, ICanBeNotifiedWhenPartitionReplayed
    {
        public Task BeginReplay() => Record("begin");

        public Task EndReplay() => Record("end");

        public Task BeginReplayPartition(Partition partition) => Record($"begin-partition:{partition}");

        public Task EndReplayPartition(Partition partition) => Record($"end-partition:{partition}");

        Task Record(string notification)
        {
            notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
