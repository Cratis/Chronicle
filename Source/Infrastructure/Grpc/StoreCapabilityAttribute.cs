// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grpc;

/// <summary>
/// Attribute used to specify the capability a caller is decided for at the event store (and namespace) a query addresses.
/// </summary>
/// <param name="capability">The capability the query exercises, as the kernel knows it.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StoreCapabilityAttribute(string capability) : Attribute
{
    /// <summary>
    /// Gets the capability the query exercises.
    /// </summary>
    public string Capability => capability;
}
