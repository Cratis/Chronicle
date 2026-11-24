// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="ChildrenOrdering"/> and its <see cref="ChildrenOrderingSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class ChildrenOrderingSurrogateConverter : IConverter<ChildrenOrdering, ChildrenOrderingSurrogate>
{
    /// <inheritdoc/>
    public ChildrenOrdering ConvertFromSurrogate(in ChildrenOrderingSurrogate surrogate) => new(surrogate.Placement, surrogate.SortBy, surrogate.Direction);

    /// <inheritdoc/>
    public ChildrenOrderingSurrogate ConvertToSurrogate(in ChildrenOrdering value) => new(value.Placement, value.SortBy, value.Direction);
}
