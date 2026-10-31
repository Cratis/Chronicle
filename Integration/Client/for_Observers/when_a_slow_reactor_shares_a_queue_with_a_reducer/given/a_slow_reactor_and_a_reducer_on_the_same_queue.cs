// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Observers.when_a_slow_reactor_shares_a_queue_with_a_reducer.given;

public class a_slow_reactor_and_a_reducer_on_the_same_queue(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
{
    public GatedReactor Reactor;
    public CountingReducer Reducer;

    public override IEnumerable<Type> EventTypes => [typeof(ItemRegistered)];
    public override IEnumerable<Type> Reactors => [typeof(GatedReactor)];
    public override IEnumerable<Type> Reducers => [typeof(CountingReducer)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reactor = new GatedReactor();
        Reducer = new CountingReducer();
        services.AddSingleton(Reactor);
        services.AddSingleton(Reducer);
    }
}
