// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_a_model_is_registered_by_type_at_runtime : given.self_reducers
{
    IReducerHandler _handler;

    async Task Establish()
    {
        _store = CreateStore();
        await _store.DiscoverAll();
    }

    async Task Because() => _handler = await _store.Reducers.Register(typeof(PlainSelf));

    [Fact] void should_accept_the_structural_model_without_a_nomination() => _handler.ReadModelType.ShouldEqual(typeof(PlainSelf));
    [Fact] void should_use_the_same_self_invoker() => _handler.Invoker.ShouldBeOfExactType<SelfReducerInvoker>();
}
