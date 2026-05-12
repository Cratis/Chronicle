// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents a nested single object property that has been cleared (set to null) on a parent.
/// </summary>
/// <param name="NestedProperty">The property path to the nested object on the parent.</param>
/// <param name="ArrayIndexers">The array indexers needed to resolve the correct element when the nested object lives inside a child collection.</param>
public record NestedCleared(PropertyPath NestedProperty, ArrayIndexers ArrayIndexers) : Change((object)null!);
