// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_a_discovered_model_is_also_nominated : given.self_reducers
{
    void Establish()
    {
        _artifacts.Reducers.Returns([typeof(MarkedSelf)]);
        _options.ExplicitArtifacts.RegisterReducer<MarkedSelf>();
        _store = CreateStore();
    }

    Task Because() => _store.DiscoverAll();

    [Fact] void should_have_one_handler() => _store.Reducers.GetAllHandlers().Count().ShouldEqual(1);
    [Fact] void should_default_to_the_full_model_name() => _store.Reducers.GetHandlerFor<MarkedSelf>().Id.Value.ShouldEqual(typeof(MarkedSelf).FullName);
}
