// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The access each kernel command needs at the event store it addresses, computed once at startup.
/// </summary>
static class StoreAccessMap
{
    static readonly FrozenDictionary<Type, StoreAccess> _commands = typeof(StoreAccessMap).Assembly.GetTypes()
        .Select(type => (Type: type, Attribute: type.GetCustomAttribute<StoreAccessAttribute>(false)))
        .Where(_ => _.Attribute is not null)
        .ToFrozenDictionary(_ => _.Type, _ => _.Attribute!.Access);

    /// <summary>
    /// Gets the access a command needs.
    /// </summary>
    /// <param name="commandType">The type of the command.</param>
    /// <returns>The declared access, or <see cref="StoreAccess.Manage"/> when none is declared.</returns>
    internal static StoreAccess ForCommand(Type commandType) => _commands.TryGetValue(commandType, out var access) ? access : StoreAccess.Manage;

    /// <summary>
    /// Gets the capability that stands for an access.
    /// </summary>
    /// <param name="access">A single access.</param>
    /// <returns>The capability.</returns>
    internal static Capability CapabilityFor(StoreAccess access) => access switch
    {
        StoreAccess.Read => WellKnownCapabilities.ReadEventStore,
        StoreAccess.Append => WellKnownCapabilities.AppendEventStore,
        _ => WellKnownCapabilities.ManageEventStore
    };
}
