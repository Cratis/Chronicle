// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.given;

public class a_reactor_observing_an_event(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
{
    public TaskCompletionSource Tcs;
    public SomeReactor Reactor;
    public IObserver ReactorObserver;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public override IEnumerable<Type> Reactors => [typeof(SomeReactor)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Reactor = new SomeReactor(Tcs);
        services.AddSingleton(Reactor);
    }

    void Establish()
    {
        ReactorObserver = GetObserverFor<SomeReactor>();
    }
}
