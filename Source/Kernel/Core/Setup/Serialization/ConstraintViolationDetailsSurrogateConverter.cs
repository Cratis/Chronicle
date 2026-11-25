// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between <see cref="ConstraintViolationDetails"/> and its <see cref="ConstraintViolationDetailsSurrogate"/>.
/// </summary>
/// <remarks>
/// The type only gives a dictionary a name, so it travels as the dictionary and is rebuilt on the other side.
/// </remarks>
[RegisterConverter]
public sealed class ConstraintViolationDetailsSurrogateConverter : IConverter<ConstraintViolationDetails, ConstraintViolationDetailsSurrogate>
{
    /// <inheritdoc/>
    public ConstraintViolationDetails ConvertFromSurrogate(in ConstraintViolationDetailsSurrogate surrogate) => new(surrogate.Details);

    /// <inheritdoc/>
    public ConstraintViolationDetailsSurrogate ConvertToSurrogate(in ConstraintViolationDetails value) => new(new Dictionary<string, string>(value));
}
