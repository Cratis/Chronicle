// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_a_classic_reducer_targets_itself : given.self_reducers
{
    ReduceResult _result;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterReducer<ClassicOwn>();
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(ClassicOwn)).Returns(new ActivatedArtifact(new ClassicOwn(0), typeof(ClassicOwn), NullLogger<ActivatedArtifact>.Instance));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        var handler = _store.Reducers.GetHandlerFor<ClassicOwn>();
        _result = await handler.Invoker.Invoke(_provider, [new(new OwnershipEvent(3), EventContext.Empty)], null);
    }

    [Fact] void should_keep_the_classic_invoker() => _store.Reducers.GetHandlerFor<ClassicOwn>().Invoker.ShouldBeOfExactType<ReducerInvoker>();
    [Fact] void should_target_the_declared_model_without_a_conflict() => _store.Reducers.GetHandlerFor<ClassicOwn>().ReadModelType.ShouldEqual(typeof(ClassicOwn));
    [Fact] void should_call_the_instance_handler_not_the_static_helper() => ((ClassicOwn)_result.ReadModelState!).Value.ShouldEqual(3);

    public class ClassicOwn(int value) : IReducerFor<ClassicOwn>
    {
        public int Value { get; } = value;
        public ClassicOwn Apply(OwnershipEvent @event, ClassicOwn? current) => new((current?.Value ?? 0) + @event.Value);
        public static ClassicOwn Reduce(OwnershipEvent @event, ClassicOwn? current) => new(999);
    }
}
