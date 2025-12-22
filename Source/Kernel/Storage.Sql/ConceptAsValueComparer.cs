// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Value comparer for ConceptAs types that compares based on the underlying primitive value.
/// </summary>
/// <typeparam name="TConcept">The ConceptAs type.</typeparam>
/// <typeparam name="TValue">The underlying primitive type.</typeparam>
/// <param name="getValue">Function to extract the primitive value from the concept.</param>
public class ConceptAsValueComparer<TConcept, TValue>(Func<TConcept, TValue> getValue) : ValueComparer<TConcept>(
    (l, r) => EqualityComparer<TValue>.Default.Equals(getValue(l!), getValue(r!)),
    v => getValue(v!).GetHashCode(),
    v => v)
    where TValue : notnull;
