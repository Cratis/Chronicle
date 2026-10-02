// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_the_same_reducer_types_again : given.all_dependencies
{
    readonly List<RegisterReducer> _registrations = [];
    CountingType _normalized;
    CountingType _unsupported;

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _normalized = new CountingType(for_ReducerFingerprint.given.CompiledReducer.Compile(for_ReducerFingerprint.given.CompiledReducer.Simple), false);
        _unsupported = new CountingType(for_ReducerFingerprint.given.CompiledReducer.Compile(for_ReducerFingerprint.given.CompiledReducer.Simple), true);
        AddHandler(_normalized, typeof(object), "normalized");
        AddHandler(_unsupported, typeof(string), "unsupported");
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _registrations.Add(message.Content.Value0!));
            return Observable.Empty<ReduceOperationMessage>();
        });
    }

    async Task Because()
    {
        await _reducers.Register();

        // Reuse the same reducer types in a fresh registration, as when handlers are recreated on reconnect.
        await CreateReducers().Register();
    }

    [Fact] void should_register_every_reducer_again() => _registrations.Count.ShouldEqual(4);
    [Fact] void should_normalize_a_supported_reducer_only_once() => _normalized.FingerprintAttempts.ShouldEqual(1);
    [Fact] void should_attempt_an_unsupported_reducer_only_once() => _unsupported.FingerprintAttempts.ShouldEqual(1);
    [Fact] void should_reuse_the_normalized_fingerprint() => HashesFor("normalized").Distinct().Count().ShouldEqual(1);
    [Fact] void should_reuse_the_fallback_fingerprint() => HashesFor("unsupported").Distinct().Count().ShouldEqual(1);

    IEnumerable<string> HashesFor(string id) => _registrations.Where(_ => _.Reducer.ReducerId == id).Select(_ => _.Reducer.Hash);

    void AddHandler(Type reducerType, Type readModelType, string id)
    {
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(reducerType);
        handler.ReadModelType.Returns(readModelType);
        handler.Id.Returns((ReducerId)id);
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([]);
        handler.IsActive.Returns(true);
        _handlersByModelType[readModelType] = handler;
    }

    class CountingType(Type type, bool unsupported) : TypeDelegator(type)
    {
        public int FingerprintAttempts { get; private set; }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            FingerprintAttempts++;
            if (unsupported) throw new UnsupportedReducerFingerprintOperand("spec operand");
            return base.GetMethods(bindingAttr);
        }
    }
}
