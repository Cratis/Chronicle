// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Azure.Core;
using Azure.Identity;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Supplies PostgreSQL passwords from Entra, sharing a token until shortly before it expires.
/// </summary>
/// <param name="credential">The credential used to acquire tokens.</param>
/// <param name="timeProvider">The clock used to check token expiration.</param>
internal sealed class PostgreSqlEntraAuthentication(TokenCredential credential, TimeProvider timeProvider) : IDisposable
{
    static readonly TokenRequestContext _request = new(["https://ossrdbms-aad.database.windows.net/.default"]);
    static readonly TimeSpan _refreshBeforeExpiry = TimeSpan.FromMinutes(5);
    readonly SemaphoreSlim _refresh = new(1, 1);
    AccessToken _token;

    /// <inheritdoc/>
    public void Dispose() => _refresh.Dispose();

    /// <summary>
    /// Creates authentication only when explicitly enabled.
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <returns>The authentication provider, or null when disabled.</returns>
    internal static PostgreSqlEntraAuthentication? From(Configuration.PostgreSqlEntra options) => options.Enabled
        ? new(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = options.ManagedIdentityClientId }), TimeProvider.System)
        : null;

    /// <summary>
    /// Configures a password provider invoked for each new physical connection, not just at startup.
    /// </summary>
    /// <param name="builder">The data source builder.</param>
    internal void Configure(NpgsqlDataSourceBuilder builder) => builder.UsePasswordProvider(
        _ => Password().AsTask().GetAwaiter().GetResult(),
        (_, cancellationToken) => Password(cancellationToken));

    /// <summary>
    /// Gets a cached token, refreshing it before expiration. Concurrent requests share the refresh.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token to use as the password.</returns>
    internal async ValueTask<string> Password(CancellationToken cancellationToken = default)
    {
        await _refresh.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_token.ExpiresOn <= timeProvider.GetUtcNow() + _refreshBeforeExpiry)
            {
                _token = await credential.GetTokenAsync(_request, cancellationToken).ConfigureAwait(false);
            }

            return _token.Token;
        }
        finally
        {
            _refresh.Release();
        }
    }

    /// <summary>
    /// Prepares a short-lived, directly opened connection for startup inspection or provisioning.
    /// </summary>
    /// <param name="connection">The connection to prepare.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Prepare(DbConnection connection)
    {
        if (connection is NpgsqlConnection)
        {
            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString) { Password = await Password() };
            connection.ConnectionString = builder.ConnectionString;
        }
    }
}
