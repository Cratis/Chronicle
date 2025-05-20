// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_Reactors.given;

public class a_disconnected_reactor_observing_no_event_types(ChronicleFixture ChronicleFixture) : IntegrationSpecificationContext(ChronicleFixture)
{
    public TaskCompletionSource Tcs;
    public ReactorWithoutHandlers Reactor;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(SomeOtherEvent)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reactor = new();
        services.AddSingleton(Reactor);
    }
}
