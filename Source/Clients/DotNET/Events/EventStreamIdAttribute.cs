// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to specify the <see cref="EventStreamId"/> for a reactor or command.
/// </summary>
/// <remarks>
/// When applied to a reactor, the specified stream ID is used for side-effect events returned
/// from handler methods when no explicit <see cref="EventStreamId"/> is set on the
/// individual side-effect. Set <paramref name="concurrency"/> to <see langword="true"/> to
/// include this value in the concurrency scope when appending events.
/// </remarks>
/// <param name="value">
/// The <see cref="EventStreamId"/> value, or <see langword="null"/> / empty string to use
/// <see cref="EventStreamId.NotSet"/> (indicating the value should be provided dynamically via
/// an interface such as <c>ICanProvideEventStreamId</c>).
/// </param>
/// <param name="concurrency">
/// Whether to include this metadata in the concurrency scope when appending events.
/// Default is <see langword="false"/>.
/// </param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventStreamIdAttribute(string? value = null, bool concurrency = false) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventStreamId"/> value.
    /// </summary>
    public EventStreamId Value { get; } = value ?? EventStreamId.NotSet;

    /// <summary>
    /// Gets a value indicating whether this metadata should be included in the concurrency scope.
    /// </summary>
    public bool Concurrency { get; } = concurrency;
}
