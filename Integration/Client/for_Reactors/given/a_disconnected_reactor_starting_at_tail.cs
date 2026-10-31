// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Reactors.given;

public class a_disconnected_reactor_starting_at_tail(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    public ReactorStartingAtTail Reactor;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reactor = new();
        services.AddSingleton(Reactor);
    }
}
