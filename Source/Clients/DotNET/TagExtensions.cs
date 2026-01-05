// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle;

/// <summary>
/// Extension methods for working with tags.
/// </summary>
public static class TagExtensions
{
    /// <summary>
    /// Get all tags from a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>Collection of tag values.</returns>
    public static IEnumerable<string> GetTags(this Type type)
    {
        var tagAttributes = type.GetCustomAttributes<TagAttribute>();
        var tagsAttributes = type.GetCustomAttributes<TagsAttribute>();

        return tagAttributes.SelectMany(_ => _.Tags)
            .Concat(tagsAttributes.SelectMany(_ => _.Tags));
    }
}
