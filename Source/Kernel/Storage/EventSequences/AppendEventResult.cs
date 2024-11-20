// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using OneOf;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents the result of appending an event to the <see cref="IEventSequenceStorage"/>.
/// </summary>
[GenerateOneOf]
public partial class AppendEventResult : OneOfBase<AppendedEvent, AppendEventError>
{
    /// <summary>
    /// Whether the event was successfully appended.
    /// </summary>
    /// <returns>True if appended, false if not.</returns>
    public bool IsSuccess => IsT0;
}
