// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Services.Clients;
using Cratis.Chronicle.Services.Compliance;
using Cratis.Chronicle.Services.Observation;
using Cratis.Chronicle.Services.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Services.Observation.Reactors;
using Cratis.Chronicle.Services.Observation.Reducers;
using Cratis.Chronicle.Services.ReadModels;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.given;

/// <summary>
/// Describes how every public method of the hand-written gRPC services treats a request for the System event store.
/// </summary>
public static class hand_written_routing_surface
{
    /// <summary>The caller must be an administrator.</summary>
    public const string Admin = "admin";

    /// <summary>The request is refused for every caller.</summary>
    public const string Refused = "refused";

    /// <summary>The method is not routed by an event store; the reason follows the prefix.</summary>
    public const string Exempt = "exempt: ";

    /// <summary>
    /// The services that are written by hand, by implementation type.
    /// </summary>
    public static readonly Type[] HandWritten =
    [
        typeof(Observers), typeof(FailedPartitions), typeof(MaterializedReadModels), typeof(ReadModels.ReadModels),
        typeof(Projections.Projections), typeof(ComplianceService), typeof(Reactors), typeof(Reducers),
        typeof(EventStoreSubscriptions), typeof(ConnectionService), typeof(Events.Constraints.Constraints)
    ];

    /// <summary>
    /// The classification of every public method, keyed by service and method name.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Classification = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Constraints.Register"] = Refused,
        ["Observers.RetryPartition"] = Admin,
        ["Observers.ClearPartitionQuarantine"] = Admin,
        ["Observers.FastForward"] = Admin,
        ["Observers.Replay"] = Admin,
        ["Observers.ReplayPartition"] = Admin,
        ["Observers.WaitForCompletion"] = Admin,
        ["Observers.ClearObserverQuarantine"] = Admin,
        ["Observers.ClearFailedPartitions"] = Admin,
        ["Observers.RetryFailedPartitions"] = Admin,
        ["Observers.RemoveObserver"] = Refused,
        ["Observers.GetObserverInformation"] = Admin,
        ["Observers.GetConnectedClientsForObserver"] = Admin,
        ["Observers.GetObservers"] = Admin,
        ["Observers.ObserveObservers"] = Admin,
        ["Observers.GetReplayableObserversForEventTypes"] = Admin,
        ["FailedPartitions.GetFailedPartitions"] = Admin,
        ["FailedPartitions.ObserveFailedPartitions"] = Admin,
        ["MaterializedReadModels.GetInstances"] = Admin,
        ["MaterializedReadModels.ObserveInstances"] = Admin,
        ["ReadModels.RegisterMany"] = Refused,
        ["ReadModels.RegisterSingle"] = Refused,
        ["ReadModels.UpdateDefinition"] = Refused,
        ["ReadModels.GetDefinitions"] = Admin,
        ["ReadModels.GetOccurrences"] = Admin,
        ["ReadModels.GetInstances"] = Admin,
        ["ReadModels.GetInstanceByKey"] = Admin,
        ["ReadModels.GetAllInstances"] = Admin,
        ["ReadModels.Watch"] = Admin,
        ["ReadModels.DehydrateSession"] = Admin,
        ["Projections.Register"] = Refused,
        ["Projections.Save"] = Refused,
        ["Projections.GetAllDefinitions"] = Admin,
        ["Projections.GetAllDeclarations"] = Admin,
        ["Projections.Preview"] = Admin,
        ["ComplianceService.Release"] = Admin,
        ["ComplianceService.DeleteEncryptionKey"] = Admin,
        ["ComplianceService.AllowNewEncryptionKey"] = Admin,
        ["ComplianceService.DeleteEncryptionKeyWithCoverage"] = Admin,
        ["ComplianceService.AllowNewEncryptionKeyWithCoverage"] = Admin,
        ["Reactors.Observe"] = Exempt + "registration stream; the registration message is refused for System (see the registration spec)",
        ["Reactors.HasReactor"] = Admin,
        ["Reducers.Observe"] = Exempt + "registration stream; the registration message is refused for System (see when_a_reducer_registers_for_the_system_store)",
        ["EventStoreSubscriptions.Add"] = Exempt + "refuses the reserved System store already",
        ["EventStoreSubscriptions.Remove"] = Exempt + "refuses the reserved System store already",
        ["EventStoreSubscriptions.GetSubscriptions"] = Refused,
        ["ConnectionService.Connect"] = Exempt + "connection handshake; addresses no store",
        ["ConnectionService.ConnectionKeepAlive"] = Exempt + "connection keepalive; addresses no store",
        ["ConnectionService.GetDescriptorSet"] = Exempt + "service metadata; addresses no store",
        ["ConnectionService.CheckCompatibility"] = Exempt + "version check; addresses no store",
        ["ConnectionService.GetConnectedClients"] = Exempt + "lists connected clients; addresses no store",
        ["ConnectionService.ObserveConnectedClients"] = Exempt + "lists connected clients; addresses no store"
    };

    /// <summary>
    /// Gets the public methods a service declares.
    /// </summary>
    /// <param name="service">The service implementation type.</param>
    /// <returns>The methods.</returns>
    public static IEnumerable<MethodInfo> MethodsOf(Type service) =>
        service.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(method => !method.IsSpecialName);

    /// <summary>
    /// Gets the key a method is classified under.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>The classification key.</returns>
    public static string KeyOf(MethodInfo method) => $"{method.DeclaringType!.Name}.{method.Name}";

    /// <summary>
    /// Creates a service whose dependencies are substitutes, so that a refused call can be shown not to reach any of them.
    /// </summary>
    /// <param name="service">The service implementation type.</param>
    /// <returns>The service.</returns>
    public static object Create(Type service)
    {
        var constructor = service.GetConstructors().Single();
        var arguments = constructor.GetParameters().Select(parameter => ArgumentFor(parameter.ParameterType)).ToArray();
        return constructor.Invoke(arguments);
    }

    static object? ArgumentFor(Type type)
    {
        if (type == typeof(JsonSerializerOptions)) return new JsonSerializerOptions();
        if (type == typeof(IOptions<Configuration.ChronicleOptions>)) return Options.Create(new Configuration.ChronicleOptions());
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            var loggerType = typeof(NullLogger<>).MakeGenericType(type.GetGenericArguments());
            var field = loggerType.GetField(nameof(NullLogger<object>.Instance), BindingFlags.Public | BindingFlags.Static);
            Assert.True(field is not null, $"Expected {loggerType.FullName}.Instance to be a public static field");
            var logger = field.GetValue(null);
            Assert.True(logger is not null, $"Expected {loggerType.FullName}.Instance to contain a logger");
            return logger;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IActivitySource<>))
        {
            return Activator.CreateInstance(typeof(ActivitySource<>).MakeGenericType(type.GetGenericArguments()));
        }

        return type.IsInterface ? Substitute.For([type], []) : null;
    }
}
