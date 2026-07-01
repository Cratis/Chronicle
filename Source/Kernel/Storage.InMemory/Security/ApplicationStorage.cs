// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.InMemory.Security;

/// <summary>
/// Represents an in-memory implementation of <see cref="IApplicationStorage"/>.
/// </summary>
public sealed class ApplicationStorage : IApplicationStorage, IDisposable
{
    readonly ConcurrentDictionary<ApplicationId, Application> _applications = new();
    readonly ReplaySubject<IEnumerable<Application>> _subject = new(1);

    /// <inheritdoc/>
    public ISubject<IEnumerable<Application>> ObserveAll()
    {
        Publish();
        return _subject;
    }

    /// <inheritdoc/>
    public Task<Application?> GetById(ApplicationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_applications.TryGetValue(id, out var application) ? application : null);

    /// <inheritdoc/>
    public Task<Application?> GetByClientId(ClientId clientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_applications.Values.FirstOrDefault(_ => _.ClientId == clientId));

    /// <inheritdoc/>
    public Task Create(Application application, CancellationToken cancellationToken = default)
    {
        _applications[application.Id] = application;
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Update(Application application, CancellationToken cancellationToken = default)
    {
        _applications[application.Id] = application;
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(ApplicationId id, CancellationToken cancellationToken = default)
    {
        _applications.TryRemove(id, out _);
        Publish();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Application>> GetAll(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Application>>([.. _applications.Values]);

    /// <inheritdoc/>
    public Task<long> Count(CancellationToken cancellationToken = default) =>
        Task.FromResult<long>(_applications.Count);

    /// <inheritdoc/>
    public Task<IEnumerable<Application>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        IEnumerable<Application> applications = [.. _applications.Values];
        if (offset is not null)
        {
            applications = applications.Skip(offset.Value);
        }

        if (count is not null)
        {
            applications = applications.Take(count.Value);
        }

        return Task.FromResult<IEnumerable<Application>>([.. applications]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Application>> FindByRedirectUri(string redirectUri, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Application>>([.. _applications.Values.Where(_ => _.RedirectUris.Contains(redirectUri))]);

    /// <inheritdoc/>
    public Task<IEnumerable<Application>> FindByPostLogoutRedirectUri(string postLogoutRedirectUri, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Application>>([.. _applications.Values.Where(_ => _.PostLogoutRedirectUris.Contains(postLogoutRedirectUri))]);

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    void Publish() => _subject.OnNext([.. _applications.Values]);
}
