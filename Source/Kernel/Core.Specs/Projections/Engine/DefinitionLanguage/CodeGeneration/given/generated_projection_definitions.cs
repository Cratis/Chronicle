// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given;

public static class generated_projection_definitions
{
    public static Contracts.Projections.ProjectionDefinition Declarative(Assembly assembly)
    {
        var readModel = assembly.GetType("Order")!;
        var projection = assembly.GetType("OrderProjection")!;
        var builderType = typeof(Client::Cratis.Chronicle.Projections.ProjectionBuilderFor<>).MakeGenericType(readModel);
        var builder = Activator.CreateInstance(builderType, new Client::Cratis.Chronicle.Projections.ProjectionId("OrderProjection"), projection, new CamelCaseNamingPolicy(), EventTypes(assembly), new JsonSerializerOptions())!;
        projection.GetMethod("Define")!.Invoke(Activator.CreateInstance(projection), [builder]);
        return (Contracts.Projections.ProjectionDefinition)builderType.GetMethod("Build", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(builder, null)!;
    }

    public static Contracts.Projections.ProjectionDefinition ModelBound(Assembly assembly, string readModelName = "Order")
    {
        var builderType = typeof(Client::Cratis.Chronicle.Projections.IProjectionFor<>).Assembly.GetType("Cratis.Chronicle.Projections.ModelBound.ModelBoundProjectionBuilder")!;
        var builder = Activator.CreateInstance(builderType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [new CamelCaseNamingPolicy(), EventTypes(assembly), null], null)!;
        return (Contracts.Projections.ProjectionDefinition)builderType.GetMethod("Build")!.Invoke(builder, [assembly.GetType(readModelName)!])!;
    }

    static Client::Cratis.Chronicle.Events.IEventTypes EventTypes(Assembly assembly)
    {
        var eventTypes = Substitute.For<Client::Cratis.Chronicle.Events.IEventTypes>();
        eventTypes.AllClrTypes.Returns(assembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(Client::Cratis.Chronicle.Events.EventTypeAttribute))).ToImmutableList());
        eventTypes.GetEventTypeFor(Arg.Any<Type>()).Returns(call => new Client::Cratis.Chronicle.Events.EventType(new Client::Cratis.Chronicle.Events.EventTypeId(((Type)call[0]).Name), Client::Cratis.Chronicle.Events.EventTypeGeneration.First));
        return eventTypes;
    }
}
