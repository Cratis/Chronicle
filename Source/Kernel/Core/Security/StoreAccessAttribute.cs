// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Declares the kind of access a command needs at the event store it addresses. A command without the attribute needs <see cref="StoreAccess.Manage"/>.
/// </summary>
/// <param name="access">The access the command needs.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class StoreAccessAttribute(StoreAccess access) : Attribute
{
    /// <summary>
    /// Gets the access the command needs.
    /// </summary>
    public StoreAccess Access { get; } = access;
}
