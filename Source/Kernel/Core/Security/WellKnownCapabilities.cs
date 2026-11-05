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
    /// Gets the capabilities decided for an event store namespace.
    /// </summary>
    internal static readonly IReadOnlySet<Capability> StoreScoped = new HashSet<Capability>
    {
        ReleaseCompliance,
        ManageComplianceKeys
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
}
