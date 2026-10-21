// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_without_dispatchable_methods : given.all_dependencies
{
    readonly List<RegisterReducer> _registrations = [];
    Exception _error;

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _clientArtifacts.Reducers.Returns([typeof(SkippedReducer), typeof(ValidReducer)]);
        _eventTypes.AllClrTypes.Returns([typeof(KnownEvent)]);
        _eventTypes.GetEventTypeFor(typeof(KnownEvent)).Returns(new EventType("known-event", 1));
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _registrations.Add(message.Content.Value0!));
            return Observable.Empty<ReduceOperationMessage>();
        });
    }

    async Task Because()
    {
        await _reducers.Discover();
        _error = await Catch.Exception(_reducers.Register);
    }

    [Fact] void should_not_fail_registration() => _error.ShouldBeNull();
    [Fact] void should_only_register_the_valid_reducer() => _registrations.Select(_ => _.Reducer.ReducerId).ShouldContainOnly(typeof(ValidReducer).GetReducerId().Value);
    [Fact] void should_warn_about_the_skipped_reducer() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Warning && call.GetArguments()[2]!.ToString()!.Contains(nameof(SkippedReducer))).ShouldBeTrue();
    [Fact] void should_explain_the_skipped_method() => _logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[2]!.ToString()!.Contains("first parameter must be a known event type")).ShouldBeTrue();

    public record Model;
    public record OtherModel;
    [EventType] public record KnownEvent;
    public class SkippedReducer : IReducerFor<Model>
    {
        public Model Reduce(Model? current, object @event) => current ?? new();
    }
    public class ValidReducer : IReducerFor<OtherModel>
    {
        public OtherModel Reduce(KnownEvent @event, OtherModel? current) => current ?? new();
    }
}
