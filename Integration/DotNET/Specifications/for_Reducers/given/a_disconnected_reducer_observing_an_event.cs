// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers.given;

public class a_disconnected_reducer_observing_an_event(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    public TaskCompletionSource Tcs;
    public ReducerWithoutDelay Reducer;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reducer = new();

        services.AddSingleton(Reducer);
    }
}
