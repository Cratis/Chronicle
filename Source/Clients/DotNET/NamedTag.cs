// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Associates a name with an exact, opaque event tag value.
/// </summary>
public record NamedTag
{
    /// <summary>
    /// Initializes a named tag.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The opaque value, which may be empty.</param>
    public NamedTag(TagName name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <exception cref="InvalidNamedTag">The name is null or blank.</exception>
    public TagName Name
    {
        get;
        init
        {
            if (value is null || string.IsNullOrWhiteSpace(value.Value))
            {
                throw new InvalidNamedTag();
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets the exact value.
    /// </summary>
    /// <exception cref="InvalidNamedTag">The value is null.</exception>
    public string Value
    {
        get;
        init
        {
            if (value is null)
            {
                throw new InvalidNamedTag();
            }

            field = value;
        }
    }
}
