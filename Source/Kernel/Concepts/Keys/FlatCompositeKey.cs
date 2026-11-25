// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys;

/// <summary>
/// Represents a flat (untyped) composite <see cref="Key"/>: the sorted identity parts of a read model instance,
/// each of them an ordinary root property of the read model.
/// </summary>
/// <remarks>
/// The <see cref="Key.Value"/> is an <see cref="ExpandoObject"/> whose members are in ordinal part name order, so
/// sinks that already handle document keys keep working. The distinct runtime type lets a sink tell a flat key from
/// a typed composite key without looking anything up.
/// </remarks>
public record FlatCompositeKey : Key
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FlatCompositeKey"/> record.
    /// </summary>
    /// <param name="parts">The resolved parts of the key, ordered by part name.</param>
    /// <param name="arrayIndexers">Any array indexers.</param>
    public FlatCompositeKey(ExpandoObject parts, ArrayIndexers arrayIndexers)
        : base(parts, arrayIndexers)
    {
    }

    /// <summary>
    /// Gets the parts of the key.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parts => ((IDictionary<string, object?>)Value).AsReadOnly();
}
