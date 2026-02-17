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
    /// Get the current system version.
    /// </summary>
    /// <returns>The current <see cref="SemanticVersion"/> or null if not set.</returns>
    Task<SemanticVersion?> GetVersion();

    /// <summary>
    /// Set the current system version.
    /// </summary>
    /// <param name="version">The <see cref="SemanticVersion"/> to set.</param>
    /// <returns>Awaitable task.</returns>
    Task SetVersion(SemanticVersion version);
}
