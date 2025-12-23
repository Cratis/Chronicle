// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents a no-op implementation of <see cref="ITokenProvider"/> for when authentication is disabled.
/// </summary>
public class NoOpTokenProvider : ITokenProvider
{
    /// <inheritdoc/>
    public Task<string?> GetAccessToken(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}
