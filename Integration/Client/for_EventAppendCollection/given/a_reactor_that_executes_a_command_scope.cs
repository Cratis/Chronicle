// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection.given;

public class a_reactor_that_executes_a_command_scope(ChronicleFixture fixture) : Specification(fixture)
{
    public EventSourceId EventSourceId;
    public IEventAppendCollection AppendedEventsCollector;

    public override IEnumerable<Type> EventTypes => [typeof(AnEventHappened), typeof(ACommandHandledEvent)];
    public override IEnumerable<Type> Reactors => [typeof(AReactorThatExecutesACommand)];

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddSingleton<AReactorThatExecutesACommand>();

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            // Commands need the full Arc core setup, not just AddCratisCommands. Register it on the host:
            // the mutable per-test registry does not preserve Arc's keyed activity sources or singleton lifetimes.
            // The in-process kernel may already have installed the same core services.
            if (services.All(descriptor => descriptor.ServiceType != typeof(IAuthorizationPolicyRuntime)))
            {
                services.AddCratisArcCore();
            }
        });

    void Establish() => EventSourceId = EventSourceId.New();

    void Destroy() => AppendedEventsCollector?.Dispose();
}
