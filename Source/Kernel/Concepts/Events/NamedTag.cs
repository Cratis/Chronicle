// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Associates a name with an exact, opaque event tag value.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Events.NamedTag")]
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
    [Id(0)]
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
    [Id(1)]
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
