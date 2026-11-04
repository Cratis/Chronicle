// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Sequences.given;

namespace Cratis.Chronicle.Security;

public class when_classifying_the_store_routed_surface : Specification
{
    static readonly string[] _routingNames =
    [
        "EventStore", "Namespace", "EventSequenceId", "ObserverId", "ProjectionId", "ReactorId", "ReducerId",
        "WebhookId", "ExternalServiceId", "SubscriptionId", "TargetEventStore", "ReadModel"
    ];

    static readonly Type[] _readModels = typeof(Administrator).Assembly.GetTypes()
        .Where(type => type.IsDefined(typeof(ReadModelAttribute), false)).ToArray();

    static readonly IReadOnlyDictionary<string, string> _withoutStoreParameter = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Servers.ServerInstanceDetails"] = "lists server instances; has no store",
        ["Diagnostics.StorageAuthorizationDiagnostic"] = "lists cluster-wide storage authorization failures; has no store",
        ["Occurrences.LifecycleOccurrenceResult"] = "a result record; it has no query of its own",
        ["EventStores.EventStoreNames"] = "lists names only",
        ["Security.AdminPasswordStatus"] = "anonymous bootstrap status",
        ["DevelopmentTools.DevelopmentToolsAvailability"] = "feature availability",
        ["Seeding.EventSeedingSupport"] = "feature availability",
        ["Schemas.TypeFormat"] = "schema metadata"
    };

    public static TheoryData<Type> ReadModels()
    {
        var cases = new TheoryData<Type>();
        foreach (var type in _readModels) cases.Add(type);
        return cases;
    }

    public static TheoryData<Type> Commands()
    {
        var cases = new TheoryData<Type>();
        foreach (var type in public_store_write_commands.AllTypes) cases.Add(type);
        return cases;
    }

    [Theory]
    [MemberData(nameof(ReadModels))]
    public void should_route_the_store_by_the_parameter_name_the_query_filter_relies_on(Type readModel)
    {
        foreach (var parameter in QueryParameters(readModel))
        {
            if (IsStoreParameter(parameter) || parameter.Name!.Equals("eventStore", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Name.ShouldEqual("eventStore");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ReadModels))]
    public void should_classify_every_read_model_as_store_routed_administrator_or_exempt(Type readModel)
    {
        var routed = QueryParameters(readModel).Any(IsStoreParameter);
        var administrator = given.administrator_surface_policy.Required.TryGetValue(readModel.FullName!, out var policy) &&
            policy == WellKnownAuthorizationPolicies.Administrator;
        var exempt = _withoutStoreParameter.TryGetValue(readModel.FullName!["Cratis.Chronicle.".Length..], out var reason);
        (routed || administrator || exempt).ShouldBeTrue();
        if (exempt) string.IsNullOrWhiteSpace(reason).ShouldBeFalse();
        if (exempt) routed.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void should_cover_every_routing_property_of_a_command_with_the_filter(Type command)
    {
        foreach (var property in command.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!_routingNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) continue;
            _routingNames.Contains(property.Name).ShouldBeTrue();
            (property.PropertyType == typeof(string) || typeof(ConceptAs<string>).IsAssignableFrom(property.PropertyType)).ShouldBeTrue();
        }
    }

    static IEnumerable<ParameterInfo> QueryParameters(Type readModel) => readModel
        .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .SelectMany(method => method.GetParameters());

    static bool IsStoreParameter(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(EventStoreName) ||
        (parameter.ParameterType == typeof(string) && parameter.Name == "eventStore");
}
