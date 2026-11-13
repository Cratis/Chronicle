// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Holds a scope's cached tags once, so every instrument of the scope shares them instead of copying the <see cref="TagList"/>.
/// </summary>
/// <remarks>
/// The tags are never changed after construction, so instruments may read them concurrently.
/// A <see cref="TagList"/> holds up to <see cref="InlineCapacity"/> tags inline and moves them all into a heap array
/// beyond that. A copy of the <see cref="TagList"/> copies the inline tags but shares that array, and
/// <see cref="TagList.Add(string, object?)"/> on the copy writes into the shared array while it has spare room.
/// Code that appends tags must therefore start from <see cref="ForAppending"/>, never from a copy of <see cref="Tags"/>.
/// </remarks>
/// <param name="tags">The scope tags.</param>
internal sealed class MetricScopeTags(TagList tags)
{
    /// <summary>The number of tags a <see cref="TagList"/> holds without a heap array.</summary>
    internal const int InlineCapacity = 8;

    readonly TagList _tags = tags;

    /// <summary>Gets the scope tags without copying them. Read them only; never append to a copy.</summary>
    internal ref readonly TagList Tags => ref _tags;

    /// <summary>
    /// Gets a copy of the scope tags that measurement tags can be appended to without touching the shared tags.
    /// </summary>
    /// <returns>A copy of the inline tags, or a rebuilt list with its own array when the scope tags spill.</returns>
    /// <remarks>Within the inline capacity this is a plain struct copy. A scope with more tags allocates on every call.</remarks>
    internal TagList ForAppending()
    {
        if (_tags.Count <= InlineCapacity) return _tags;

        var copy = default(TagList);
        foreach (var tag in _tags) copy.Add(tag);
        return copy;
    }
}
