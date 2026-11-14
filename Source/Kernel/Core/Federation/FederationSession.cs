// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationSession"/>. It holds only which connection is current, in memory: the cursor of
/// a session is the tail of the inbox it appends to, so nothing else needs to survive a deactivation.
/// </summary>
public sealed class FederationSession : Grain, IFederationSession
{
    string? _current;

    /// <inheritdoc/>
    public Task Open(string connectionId)
    {
        _current = connectionId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> Renew(string connectionId)
    {
        _current ??= connectionId;
        return Task.FromResult(string.Equals(_current, connectionId, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public Task Close(string connectionId)
    {
        if (string.Equals(_current, connectionId, StringComparison.Ordinal))
        {
            _current = null;
        }

        return Task.CompletedTask;
    }
}
