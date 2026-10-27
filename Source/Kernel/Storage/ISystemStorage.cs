// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.Patching;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the system storage.
/// </summary>
public interface ISystemStorage
{
    /// <summary>
    /// Gets the user storage.
    /// </summary>
    IUserStorage Users { get; }

    /// <summary>
    /// Gets the separately persisted administrator privileges.
    /// </summary>
    IAdministratorStorage Administrators { get; }

    /// <summary>
    /// Gets the application storage.
    /// </summary>
    IApplicationStorage Applications { get; }

    /// <summary>
    /// Gets the data protection key storage.
    /// </summary>
    IDataProtectionKeyStorage DataProtectionKeys { get; }

    /// <summary>
    /// Gets the patch storage.
    /// </summary>
    IPatchStorage Patches { get; }

    /// <summary>
    /// Get the system information.
    /// </summary>
    /// <returns>The <see cref="SystemInformation"/> or null if not set.</returns>
    Task<SystemInformation?> GetSystemInformation();

    /// <summary>
    /// Set the system information.
    /// </summary>
    /// <param name="systemInformation">The <see cref="SystemInformation"/> to set.</param>
    /// <returns>Awaitable task.</returns>
    Task SetSystemInformation(SystemInformation systemInformation);

    /// <summary>
    /// Gets the newest kernel version that successfully completed patching.
    /// </summary>
    /// <returns>The kernel version, or null when none has been recorded.</returns>
    Task<SemanticVersion?> GetPatchingKernelVersion();

    /// <summary>
    /// Records the newest kernel version that successfully completed patching.
    /// </summary>
    /// <param name="version">The kernel version.</param>
    /// <returns>Awaitable task.</returns>
    Task SetPatchingKernelVersion(SemanticVersion version);
}
