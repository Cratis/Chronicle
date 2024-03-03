// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Attribute to specify which event sequence to use.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventSequenceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceAttribute"/> class.
    /// </summary>
    /// <param name="sequence">String representation of a <see cref="EventSequenceId"/>.</param>
    public EventSequenceAttribute(string sequence)
    {
        Sequence = sequence;
    }

    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> to use.
    /// </summary>
    public EventSequenceId Sequence { get; }
}
