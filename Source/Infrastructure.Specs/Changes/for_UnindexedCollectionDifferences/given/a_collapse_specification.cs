// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

/// <summary>
/// Base for <see cref="UnindexedCollectionDifferences"/> collapse scenarios, providing helpers for
/// building the <see cref="ExpandoObject"/> states the differences are computed against.
/// </summary>
public class a_collapse_specification : Specification
{
    protected static ExpandoObject Expando(params (string Key, object? Value)[] properties)
    {
        var instance = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)instance;
        foreach (var (key, value) in properties)
        {
            dictionary[key] = value;
        }

        return instance;
    }

    protected static object[] Collection(params object[] items) => items;
}
