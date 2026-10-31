// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_an_unmarked_model_is_nominated : given.self_reducers
{
    IReducerHandler _handler;
    ReduceResult _result;
    Type _modelType;

    void Establish()
    {
        _modelType = SelfModelType();
        _options.ExplicitArtifacts.RegisterReducer<PlainSelf>("self-alias").RegisterReducer(_modelType, "self-alias");
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _handler = _store.Reducers.GetHandlerFor<PlainSelf>();
        _result = await _handler.Invoker.Invoke(_provider, [new(new OwnershipEvent(3), EventContext.Empty)], null);
    }

    static Type SelfModelType() => typeof(PlainSelf);

    [Fact] void should_coalesce_equal_nominations() => _store.Reducers.GetAllHandlers().Count().ShouldEqual(1);
    [Fact] void should_use_the_supplied_identity() => _handler.Id.ShouldEqual((ReducerId)"self-alias");
    [Fact] void should_use_the_model_as_its_own_reducer() => _handler.ReadModelType.ShouldEqual(typeof(PlainSelf));
    [Fact] void should_fold_without_constructing_the_model() => ((PlainSelf)_result.ReadModelState!).Value.ShouldEqual(3);
    [Fact] void should_not_activate_an_artifact() => _activator.DidNotReceive().Activate(Arg.Any<IServiceProvider>(), typeof(PlainSelf));
}
