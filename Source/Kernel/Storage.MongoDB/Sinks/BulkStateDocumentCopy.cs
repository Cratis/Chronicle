// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Objects;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Copies bulk-cache documents without changing collection runtime types used by child-value matching.
/// </summary>
internal static class BulkStateDocumentCopy
{
    /// <summary>
    /// Copies a document and its nested mutable values, retaining null for an absent state.
    /// </summary>
    /// <param name="source">The source document.</param>
    /// <returns>An independent document with the same collection runtime types.</returns>
    internal static ExpandoObject? Copy(ExpandoObject? source)
    {
        if (source is null) return null;

        var copy = new ExpandoObject();
        var properties = (IDictionary<string, object?>)copy;
        foreach (var (name, value) in source)
        {
            properties[name] = CopyValue(value);
        }

        return copy;
    }

    static object? CopyValue(object? value) => value switch
    {
        null => null,
        ExpandoObject document => Copy(document),
        Array array => CopyArray(array),
        IDictionary dictionary => CopyDictionary(dictionary),
        IList list => CopyList(list),
        _ => value.Clone()
    };

    static Array CopyArray(Array source)
    {
        var copy = (Array)source.Clone();
        var indices = Enumerable.Range(0, source.Rank).Select(source.GetLowerBound).ToArray();
        CopyArrayDimension(source, copy, indices, 0);
        return copy;
    }

    static void CopyArrayDimension(Array source, Array copy, int[] indices, int dimension)
    {
        for (var index = source.GetLowerBound(dimension); index <= source.GetUpperBound(dimension); index++)
        {
            indices[dimension] = index;
            if (dimension == source.Rank - 1)
            {
                copy.SetValue(CopyValue(source.GetValue(indices)), indices);
            }
            else
            {
                CopyArrayDimension(source, copy, indices, dimension + 1);
            }
        }
    }

    static IDictionary CopyDictionary(IDictionary source)
    {
        var copy = (IDictionary)Activator.CreateInstance(source.GetType())!;
        foreach (DictionaryEntry entry in source)
        {
            copy.Add(CopyValue(entry.Key)!, CopyValue(entry.Value));
        }

        return copy;
    }

    static IList CopyList(IList source)
    {
        var copy = (IList)Activator.CreateInstance(source.GetType())!;
        foreach (var item in source)
        {
            copy.Add(CopyValue(item));
        }

        return copy;
    }
}
