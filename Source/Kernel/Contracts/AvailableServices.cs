// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Provides all service contract types registered in Chronicle, discovered automatically via the <see cref="ServiceAttribute"/>.
/// </summary>
public static class AvailableServices
{
    /// <summary>
    /// Gets all service types that are registered.
    /// </summary>
    public static IReadOnlyList<Type> All { get; } =
        typeof(AvailableServices).Assembly
            .ExportedTypes
            .Where(_ => _.IsInterface && Attribute.IsDefined(_, typeof(ServiceAttribute)))
            .ToArray();
}
