// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Represents a gRPC service definition derived from commands and queries.
/// </summary>
/// <param name="serviceName">The name of the gRPC service (from [BelongsTo] attribute).</param>
/// <param name="serviceNamespace">The namespace where the commands and queries are defined.</param>
public class ServiceDefinition(string serviceName, string serviceNamespace)
{
    /// <summary>Gets the service name.</summary>
    public string ServiceName { get; } = serviceName;

    /// <summary>Gets the namespace.</summary>
    public string Namespace { get; } = serviceNamespace;

    /// <summary>Gets the commands belonging to this service.</summary>
    public IList<CommandDefinition> Commands { get; } = new List<CommandDefinition>();

    /// <summary>Gets the queries (read models) belonging to this service.</summary>
    public IList<QueryDefinition> Queries { get; } = new List<QueryDefinition>();
}

