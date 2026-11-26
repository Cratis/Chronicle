// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="ChildrenOrdering"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="Placement">The <see cref="ChildrenPlacement"/>.</param>
/// <param name="SortBy">The <see cref="PropertyPath"/> to sort by.</param>
/// <param name="Direction">The <see cref="SortDirection"/>.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.ChildrenOrderingSurrogate")]
public readonly record struct ChildrenOrderingSurrogate(
    [property: Id(0)] ChildrenPlacement Placement,
    [property: Id(1)] PropertyPath SortBy,
    [property: Id(2)] SortDirection Direction);
