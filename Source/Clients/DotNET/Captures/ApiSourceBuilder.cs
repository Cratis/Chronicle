// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="IApiSourceBuilder"/>.
/// </summary>
/// <param name="url">The URL to observe.</param>
public class ApiSourceBuilder(string url) : IApiSourceBuilder
{
    string? _auth;
    string? _poll;

    /// <inheritdoc/>
    public IApiSourceBuilder PollEvery(string interval)
    {
        _poll = interval;

        return this;
    }

    /// <inheritdoc/>
    public IApiSourceBuilder WithBearerToken(string token)
    {
        _auth = $"bearer {token}";

        return this;
    }

    /// <inheritdoc/>
    public IApiSourceBuilder WithAuth(string auth)
    {
        _auth = auth;

        return this;
    }

    /// <summary>
    /// Builds the <see cref="SourceDefinition"/>.
    /// </summary>
    /// <returns>A new <see cref="SourceDefinition"/>.</returns>
    public SourceDefinition Build() => new(SourceType.Api, Url: url, Poll: _poll, Auth: _auth);
}
