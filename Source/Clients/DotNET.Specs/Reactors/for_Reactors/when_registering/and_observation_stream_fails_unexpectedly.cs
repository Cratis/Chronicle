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

public class and_observation_stream_fails_unexpectedly : given.all_dependencies
{
    readonly Exception _streamFailure = new InvalidOperationException("Boom");

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"test-connection-id");

        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Throw<EventsToObserve>(_streamFailure));
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
    }

    async Task Because()
    {
        await _reactors.Register();
    }

    [Fact] void should_log_error() => HasErrorLog().ShouldBeTrue();
    [Fact] void should_wrap_stream_failure() => GetLoggedError().ShouldBeOfExactType<ReactorObservationStreamFailed>();
    [Fact] void should_preserve_inner_exception() => GetLoggedError().InnerException.ShouldEqual(_streamFailure);

    Exception GetLoggedError() =>
        _logger
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments())
            .Where(arguments => arguments.Length > 3 && arguments[0] is LogLevel level && level == LogLevel.Error)
            .Select(arguments => arguments[3] as Exception)
            .First()!;

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