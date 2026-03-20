// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grpc;

/// <summary>
/// Attribute used to specify which gRPC service a command or query belongs to.
/// </summary>
/// <param name="service">The name of the gRPC service.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BelongsToAttribute(string service) : Attribute
{
    /// <summary>
    /// Gets the name of the gRPC service this command or query belongs to.
    /// </summary>
    public string Service => service;
}
