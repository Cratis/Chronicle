// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// The exception that is thrown when types belonging to the same gRPC service are in different namespaces.
/// </summary>
/// <param name="serviceName">The name of the service with the mismatch.</param>
/// <param name="expectedNamespace">The expected namespace.</param>
/// <param name="actualNamespace">The actual namespace of the offending type.</param>
/// <param name="typeName">The name of the type causing the mismatch.</param>
public class NamespaceMismatchException(
    string serviceName,
    string expectedNamespace,
    string actualNamespace,
    string typeName)
    : Exception($"Service '{serviceName}' has namespace mismatch. Expected '{expectedNamespace}' but type '{typeName}' is in '{actualNamespace}'.")
{
}

