// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Patching;
using Cratis.Chronicle.Storage.InMemory.Security;
using Cratis.Chronicle.Storage.Patching;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="ISystemStorage"/>.
/// </summary>
public sealed class SystemStorage : ISystemStorage
{
    SystemInformation? _systemInformation;

    /// <inheritdoc/>
    public IUserStorage Users { get; } = new UserStorage();

    /// <inheritdoc/>
    public IApplicationStorage Applications { get; } = new ApplicationStorage();

    /// <inheritdoc/>
    public IDataProtectionKeyStorage DataProtectionKeys { get; } = new DataProtectionKeyStorage();

    /// <inheritdoc/>
    public IPatchStorage Patches { get; } = new PatchStorage();

    /// <inheritdoc/>
    public Task<SystemInformation?> GetSystemInformation() => Task.FromResult(_systemInformation);

    /// <inheritdoc/>
    public Task SetSystemInformation(SystemInformation systemInformation)
    {
        _systemInformation = systemInformation;
        return Task.CompletedTask;
    }
}
