// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Declares field numbers that a message has retired and must never reuse.
/// </summary>
/// <remarks>
/// A retired field number cannot be handed to a new member: an older payload still carries the old field, and a
/// reader would decode those bytes as whatever now claims the number. Reserving it makes that a compile-time
/// impossibility in every generated language rather than a review-time convention.
/// <para>
/// It exists because the schema is generated. A <c>reserved</c> line added to the generated file by hand
/// disappears the next time anyone regenerates, silently and with nothing to notice it by - which is the whole
/// hazard, since the reservation matters most long after everyone has forgotten why it is there.
/// </para>
/// </remarks>
/// <param name="fieldNumbers">The retired field numbers.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ReservedProtoFieldsAttribute(params int[] fieldNumbers) : Attribute
{
    /// <summary>
    /// Gets the retired field numbers.
    /// </summary>
    public IReadOnlyList<int> FieldNumbers { get; } = fieldNumbers;
}
