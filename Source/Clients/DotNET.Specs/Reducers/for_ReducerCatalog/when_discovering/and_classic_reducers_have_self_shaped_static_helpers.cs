// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_classic_reducers_have_self_shaped_static_helpers : given.self_reducers
{
    ReduceResult _result;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterReducer<ClassicHelper>();
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(ClassicHelper)).Returns(new ActivatedArtifact(new ClassicHelper(), typeof(ClassicHelper), NullLogger<ActivatedArtifact>.Instance));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        var handler = _store.Reducers.GetHandlerFor<ClassicHelper>();
        _result = await handler.Invoker.Invoke(_provider, [new(new OwnershipEvent(3), EventContext.Empty)], null);
    }

    [Fact] void should_keep_the_classic_invoker() => _store.Reducers.GetHandlerFor<ClassicHelper>().Invoker.ShouldBeOfExactType<ReducerInvoker>();
    [Fact] void should_ignore_the_self_shaped_static_helper() => ((OtherModel)_result.ReadModelState!).Value.ShouldEqual(3);
    [Fact] void should_not_classify_the_classic_type_as_self_reducing() => SelfReducerMethods.HasSelfShape(typeof(ClassicHelper)).ShouldBeFalse();

    public record OtherModel(int Value);

    public class ClassicHelper : IReducerFor<OtherModel>
    {
        public OtherModel Apply(OwnershipEvent @event, OtherModel? current) => new(@event.Value);
        public static ClassicHelper Reduce(OwnershipEvent @event, ClassicHelper? current) => new();
    }
}
