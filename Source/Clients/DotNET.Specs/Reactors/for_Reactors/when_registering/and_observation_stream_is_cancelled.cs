// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_observation_stream_is_cancelled : given.all_dependencies
{
    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"test-connection-id");

        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Throw<EventsToObserve>(new OperationCanceledException()));
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
    }

    async Task Because()
    {
        await _reactors.Register();
    }

    [Fact] void should_not_log_error_for_expected_cancellation() => HasErrorLog().ShouldBeFalse();

    bool HasErrorLog() =>
        _logger
            .ReceivedCalls()
            .Any(call =>
            {
                if (call.GetMethodInfo().Name != nameof(ILogger.Log))
                {
                    return false;
                }

                var arguments = call.GetArguments();
                return arguments.Length > 0 && arguments[0] is LogLevel level && level == LogLevel.Error;
            });

    class MyReactor : IReactor;
}