// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents an in-memory implementation of <see cref="IUserStorage"/>.
/// </summary>
public sealed class UserStorage : IUserStorage
{
    readonly ConcurrentDictionary<UserId, User> _users = new();
    readonly ReplaySubject<IEnumerable<User>> _subject = new(1);

    /// <inheritdoc/>
    public ISubject<IEnumerable<User>> ObserveAll()
    {
        Publish();
        return _subject;
    }

    /// <inheritdoc/>
    public Task<User?> GetById(UserId id) =>
        Task.FromResult(_users.TryGetValue(id, out var user) ? user : null);

    /// <inheritdoc/>
    public Task<User?> GetByUsername(Username username) =>
        Task.FromResult(_users.Values.FirstOrDefault(_ =>
            string.Equals(_.Username, username, StringComparison.OrdinalIgnoreCase)));

    /// <inheritdoc/>
    public Task<User?> GetByEmail(UserEmail email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Task.FromResult<User?>(null);
        }

        return Task.FromResult(_users.Values.FirstOrDefault(_ =>
            _.Email is not null && string.Equals(_.Email, email, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc/>
    public Task Create(User user)
    {
        _users[user.Id] = user;
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Update(User user)
    {
        if (_users.ContainsKey(user.Id))
        {
            _users[user.Id] = user;
            Publish();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(UserId id)
    {
        _users.TryRemove(id, out _);
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<User>> GetAll() => Task.FromResult<IEnumerable<User>>([.. _users.Values]);

    void Publish() => _subject.OnNext([.. _users.Values]);
}
