// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Holds a scope's cached tags once, so every instrument of the scope shares them instead of copying the <see cref="TagList"/>.
/// </summary>
/// <param name="tags">The scope tags.</param>
internal sealed class MetricScopeTags(TagList tags)
{
    readonly TagList _tags = tags;

    /// <summary>Gets the scope tags without copying them.</summary>
    internal ref readonly TagList Tags => ref _tags;
}
