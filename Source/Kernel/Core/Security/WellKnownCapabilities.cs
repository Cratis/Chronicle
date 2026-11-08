// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The capabilities the kernel knows. The values are part of the contract and must never be renamed.
/// </summary>
public static class WellKnownCapabilities
{
    /// <summary>
    /// Administering users, applications and administrators.
    /// </summary>
    public static readonly Capability AdministerSecurity = "chronicle.security.administer";

    /// <summary>
    /// Performing operational mutations such as jobs and observer operations.
    /// </summary>
    public static readonly Capability OperateEventStore = "chronicle.event-store.operate";

    /// <summary>
    /// Reaching the System event store through the routing guard.
    /// </summary>
    public static readonly Capability AccessSystemStore = "chronicle.event-store.access";

    /// <summary>
    /// Releasing protected content in an event store namespace.
    /// </summary>
    public static readonly Capability ReleaseCompliance = "chronicle.compliance.release";

    /// <summary>
    /// Erasing or authorizing a new encryption key in an event store namespace.
    /// </summary>
    public static readonly Capability ManageComplianceKeys = "chronicle.compliance.key-lifecycle";

    /// <summary>
    /// Reading the alert incidents of an event store.
    /// </summary>
    public static readonly Capability ReadAlerts = "chronicle.alerts.read";

    /// <summary>Reading an ordinary event store.</summary>
    public static readonly Capability ReadEventStore = "chronicle.event-store.read";

    /// <summary>Appending to an ordinary event store.</summary>
    public static readonly Capability AppendEventStore = "chronicle.event-store.append";

    /// <summary>Managing an ordinary event store.</summary>
    public static readonly Capability ManageEventStore = "chronicle.event-store.manage";

    /// <summary>
    /// Gets the capabilities decided for an event store namespace.
    /// </summary>
    internal static readonly IReadOnlySet<Capability> StoreScoped = new HashSet<Capability>
    {
        ReleaseCompliance,
        ManageComplianceKeys,
        ReadAlerts,
        ReadEventStore,
        AppendEventStore,
        ManageEventStore
    };

    /// <summary>
    /// Gets the capabilities bundled by the administrator role.
    /// </summary>
    internal static readonly IReadOnlySet<Capability> Administrator = new HashSet<Capability>
    {
        AdministerSecurity,
        OperateEventStore,
        AccessSystemStore
    };

    /// <summary>Maps store-scoped capabilities to their required access.</summary>
    /// <param name="capability">The capability.</param>
    /// <returns>The required access.</returns>
    internal static StoreAccess AccessOf(Capability capability) => capability.Value switch
    {
        "chronicle.event-store.read" or "chronicle.compliance.release" or "chronicle.alerts.read" => StoreAccess.Read,
        "chronicle.event-store.append" => StoreAccess.Append,
        "chronicle.event-store.manage" or "chronicle.compliance.key-lifecycle" => StoreAccess.Manage,
        _ => StoreAccess.None
    };
}
