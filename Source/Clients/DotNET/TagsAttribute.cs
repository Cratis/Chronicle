// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Attribute used to tag observers, read models, and event types with multiple tags.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TagsAttribute"/>.
/// </remarks>
/// <param name="tags">The tags to apply.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TagsAttribute(params string[] tags) : Attribute
{
    /// <summary>
    /// Gets the tags.
    /// </summary>
    public IEnumerable<string> Tags { get; } = tags.Where(tag => !string.IsNullOrWhiteSpace(tag));
}
