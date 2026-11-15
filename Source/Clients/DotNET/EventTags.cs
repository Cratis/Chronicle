// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle;

/// <summary>
/// Provides ambient named tags that are stamped on every event appended within the scope.
/// </summary>
/// <remarks>
/// The scope flows with the async execution context, so it can be opened around the work that appends events
/// (for example a command handler) without any of the appending code having to know about tags. The audience is
/// only stamped on events appended to the outbox, or events whose type is marked with <see cref="AudienceAttribute"/>.
/// </remarks>
public static class EventTags
{
    static readonly AsyncLocal<ImmutableList<NamedTag>> _current = new();

    /// <summary>
    /// Gets the ambient named tags in effect right now.
    /// </summary>
    internal static IReadOnlyList<NamedTag> Current => _current.Value ?? ImmutableList<NamedTag>.Empty;

    /// <summary>
    /// Opens a scope in which every appended event gets the named tag.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>An <see cref="IDisposable"/> that closes the scope when disposed.</returns>
    public static IDisposable Scope(string name, string value)
    {
        var previous = _current.Value;
        _current.Value = (previous ?? ImmutableList<NamedTag>.Empty).Add(new NamedTag(new TagName(name), value));
        return new ScopeLifetime(previous);
    }

    /// <summary>
    /// Opens a scope in which events appended to the outbox (or marked with <see cref="AudienceAttribute"/>) are addressed to the audience.
    /// </summary>
    /// <param name="value">The audience value.</param>
    /// <returns>An <see cref="IDisposable"/> that closes the scope when disposed.</returns>
    public static IDisposable Audience(string value) => Scope(WellKnownTags.Audience, value);

    sealed class ScopeLifetime(ImmutableList<NamedTag>? previous) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = previous!;
        }
    }
}
