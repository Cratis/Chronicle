// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Sinks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering;

public class with_sql_sink_configured : given.all_dependencies_with_configured_sink
{
    class MyReadModel;

    protected override SinkTypeId DefaultSinkTypeId => WellKnownSinkTypes.SQL;

    IReducerHandler _handler;
    SinkDefinition _capturedSink;

    void Establish()
    {
        _handler = Substitute.For<IReducerHandler>();
        _handler.Id.Returns(new ReducerId(Guid.NewGuid().ToString()));
        _handler.EventSequenceId.Returns(EventSequenceId.Log);
        _handler.ReadModelType.Returns(typeof(MyReadModel));
        _handler.ReducerType.Returns(typeof(MyReadModel));
        _handler.EventTypes.Returns([]);
        _handler.IsActive.Returns(true);
        _handler.CancellationToken.Returns(CancellationToken.None);

        _reducersService
            .When(_ => _.Observe(Arg.Any<IObservable<ReducerMessage>>()))
            .Do(ci =>
            {
                var observable = ci.Arg<IObservable<ReducerMessage>>();
                observable.Take(1).Subscribe(msg =>
                {
                    if (msg.Content.Value0 is RegisterReducer registration)
                    {
                        _capturedSink = registration.Reducer.Sink;
                    }
                });
            });

        var handlersByType = new Dictionary<Type, IReducerHandler> { [typeof(MyReadModel)] = _handler };
        var handlersByModelType = new Dictionary<Type, IReducerHandler> { [typeof(MyReadModel)] = _handler };
        SetField("_handlersByType", handlersByType);
        SetField("_handlersByModelType", handlersByModelType);
    }

    async Task Because() => await _reducers.Register();

    [Fact] void should_register_with_sql_sink_type() => _capturedSink.TypeId.ShouldEqual(WellKnownSinkTypes.SQL.Value);

    void SetField(string name, object value) =>
        typeof(Reducers).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_reducers, value);
}
