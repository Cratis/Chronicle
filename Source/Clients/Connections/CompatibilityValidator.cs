// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Validates compatibility between client and server gRPC contracts.
/// </summary>
internal static class CompatibilityValidator
{
    /// <summary>
    /// Validates that the client's schema is compatible with the server's schema.
    /// </summary>
    /// <param name="clientSchema">The client's schema definition.</param>
    /// <param name="serverSchema">The server's schema definition.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <returns>A <see cref="CompatibilityCheckResult"/> indicating compatibility status.</returns>
    public static CompatibilityCheckResult Validate(
        string clientSchema,
        string serverSchema,
        ILogger logger)
    {
        var errors = new List<string>();

        try
        {
            var clientServices = ParseSchemaToServices(clientSchema);
            var serverServices = ParseSchemaToServices(serverSchema);

            // Check each service the client expects
            foreach (var (serviceName, clientMethods) in clientServices)
            {
                if (!serverServices.TryGetValue(serviceName, out var serverMethods))
                {
                    errors.Add($"Service '{serviceName}' expected by client is missing on server");
                    continue;
                }

                // Check each method the client expects
                foreach (var (methodName, clientSignature) in clientMethods)
                {
                    if (!serverMethods.TryGetValue(methodName, out var serverSignature))
                    {
                        errors.Add($"Method '{methodName}' in service '{serviceName}' expected by client is missing on server");
                        continue;
                    }

                    // Check method signature compatibility
                    if (clientSignature != serverSignature)
                    {
                        errors.Add($"Method '{serviceName}.{methodName}' has incompatible signature - client: {clientSignature}, server: {serverSignature}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.FailedToValidateCompatibility(ex);
            errors.Add($"Failed to parse schemas: {ex.Message}");
        }

        return new CompatibilityCheckResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Generates the client's schema from local service contracts.
    /// </summary>
    /// <returns>String containing the schema definition.</returns>
    public static string GenerateClientSchema()
    {
        var generator = new SchemaGenerator
        {
            ProtoSyntax = ProtoSyntax.Proto3
        };

        return generator.GetSchema(Contracts.RegisteredServiceTypes.All);
    }

    static Dictionary<string, Dictionary<string, string>> ParseSchemaToServices(string schema)
    {
        var services = new Dictionary<string, Dictionary<string, string>>();
        var lines = schema.Split('\n');
        string? currentService = null;
        var currentServiceMethods = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Detect service definition
            if (trimmedLine.StartsWith("service ", StringComparison.Ordinal))
            {
                // Save previous service if any
                if (currentService is not null)
                {
                    services[currentService] = currentServiceMethods;
                }

                // Extract service name
                var parts = trimmedLine.Split([' ', '{'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    currentService = parts[1];
                    currentServiceMethods = [];
                }
            }

            // Detect RPC method definition
            else if (trimmedLine.StartsWith("rpc ", StringComparison.Ordinal) && currentService is not null)
            {
                // Extract method signature: rpc MethodName(RequestType) returns (ResponseType);
                var methodSignature = trimmedLine.TrimEnd(';');
                var rpcParts = methodSignature.Split([' ', '(', ')'], StringSplitOptions.RemoveEmptyEntries);

                if (rpcParts.Length >= 3)
                {
                    var methodName = rpcParts[1];
                    currentServiceMethods[methodName] = methodSignature;
                }
            }
        }

        // Save last service
        if (currentService is not null)
        {
            services[currentService] = currentServiceMethods;
        }

        return services;
    }
}
