// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents in-memory storage of administrator privileges.
/// </summary>
public class AdministratorStorage : IAdministratorStorage
{
    readonly ConcurrentDictionary<UserId, Administrator> _administrators = new();

    /// <inheritdoc/>
    public Task<IEnumerable<Administrator>> GetAll() => Task.FromResult<IEnumerable<Administrator>>(_administrators.Values.ToArray());

    /// <inheritdoc/>
    public Task Save(Administrator administrator)
    {
        _administrators[administrator.Id] = administrator;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(UserId userId)
    {
        _administrators.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
