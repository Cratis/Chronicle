// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Estimates how much memory a dynamic document takes.
/// </summary>
public static class ExpandoObjectSize
{
    const long ObjectOverhead = 32;
    const long EntryOverhead = 48;
    const long StringOverhead = 24;
    const int MaximumDepth = 64;

    /// <summary>
    /// Estimate the size in bytes of a document.
    /// </summary>
    /// <param name="document">The <see cref="ExpandoObject"/> to measure.</param>
    /// <returns>The estimated size in bytes.</returns>
    /// <remarks>
    /// The estimate walks the document without allocating and counts what dominates: strings at two bytes a
    /// character, plus a fixed cost for every value and entry. It is meant for deciding how much memory working on
    /// a document is likely to need, not for accounting.
    /// </remarks>
    public static long Estimate(ExpandoObject document) => EstimateValue(document, 0);

    static long EstimateValue(object? value, int depth)
    {
        if (depth > MaximumDepth)
        {
            return ObjectOverhead;
        }

        switch (value)
        {
            case null:
                return 0;

            case string text:
                return StringOverhead + (text.Length * 2L);

            case IDictionary<string, object?> dictionary:
                var size = ObjectOverhead;
                foreach (var entry in dictionary)
                {
                    size += EntryOverhead + StringOverhead + (entry.Key.Length * 2L) + EstimateValue(entry.Value, depth + 1);
                }

                return size;

            case IEnumerable items:
                var itemsSize = ObjectOverhead;
                foreach (var item in items)
                {
                    itemsSize += 8 + EstimateValue(item, depth + 1);
                }

                return itemsSize;

            default:
                return ObjectOverhead;
        }
    }
}
