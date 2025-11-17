// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.given;

public class a_reactor_observing_an_event(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    public TaskCompletionSource Tcs;
    public SomeReactor Reactor;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
    public override IEnumerable<Type> Reactors => [typeof(SomeReactor)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Reactor = new SomeReactor(Tcs);
        services.AddSingleton(Reactor);
    }
}
