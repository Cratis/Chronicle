// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Attribute to specify which event sequence to use.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceAttribute"/> class.
/// </remarks>
/// <param name="sequence">String representation of a <see cref="EventSequenceId"/>.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventSequenceAttribute(string sequence) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> to use.
    /// </summary>
    public EventSequenceId Sequence { get; } = sequence;
}
