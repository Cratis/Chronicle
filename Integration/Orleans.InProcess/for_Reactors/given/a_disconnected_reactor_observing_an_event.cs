// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.given;

public class a_disconnected_reactor_observing_an_event(ChronicleFixture ChronicleFixture) : IntegrationSpecificationContext(ChronicleFixture)
{
    public TaskCompletionSource Tcs;
    public ReactorWithoutDelay Reactor;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(SomeOtherEvent)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reactor = new();
        services.AddSingleton(Reactor);
    }
}
