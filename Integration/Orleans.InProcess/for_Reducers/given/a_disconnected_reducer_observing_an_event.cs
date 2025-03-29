// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Integration.Base;
using Xunit.Abstractions;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.given;

public class a_disconnected_reducer_observing_an_event(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
{
    public TaskCompletionSource Tcs;
    public ReducerWithoutDelay Reducer;
    public IObserver ReducerObserver;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reducer = new();

        services.AddSingleton(Reducer);
    }
}
