// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
