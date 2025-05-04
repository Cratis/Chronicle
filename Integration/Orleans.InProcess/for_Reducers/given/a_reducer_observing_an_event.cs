// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.given;

public class a_reducer_observing_an_event(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
{
    public TaskCompletionSource Tcs;
    public SomeReducer Reducer;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public override IEnumerable<Type> Reducers => [typeof(SomeReducer)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Reducer = new SomeReducer(Tcs);
        services.AddSingleton(Reducer);
    }
}
