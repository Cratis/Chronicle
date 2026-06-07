// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection.given;

public class a_reactor_that_executes_a_command_scope(ChronicleFixture fixture) : Specification(fixture)
{
    public EventSourceId EventSourceId;
    public IEventAppendCollection AppendedEventsCollector;

    public override IEnumerable<Type> EventTypes => [typeof(AnEventHappened), typeof(ACommandHandledEvent)];
    public override IEnumerable<Type> Reactors => [typeof(AReactorThatExecutesACommand)];

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddCratisCommands();
        RegisterArcActivitySource(services, "Cratis.Arc.Commands.CommandFilters");
        RegisterArcActivitySource(services, "Cratis.Arc.Commands.CommandPipeline");
        services.AddSingleton<AReactorThatExecutesACommand>();
    }

    static void RegisterArcActivitySource(IServiceCollection services, string typeName)
    {
        var type = typeof(Arc.Commands.ICommandPipeline).Assembly.GetType(typeName)!;
        var activitySourceType = typeof(Traces.ActivitySource<>).MakeGenericType(type);
        var activitySourceInterfaceType = typeof(Traces.IActivitySource<>).MakeGenericType(type);
        services.AddSingleton(activitySourceType, _ => Activator.CreateInstance(
            activitySourceType,
            new System.Diagnostics.ActivitySource(activitySourceType.FullName ?? activitySourceType.Name))!);
        services.AddSingleton(activitySourceInterfaceType, sp => sp.GetRequiredService(activitySourceType));
    }

    void Establish() => EventSourceId = EventSourceId.New();

    void Destroy() => AppendedEventsCollector?.Dispose();
}
