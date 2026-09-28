// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Narrows an event sequence query to a named tag and optionally one of its exact values.
/// </summary>
public record NamedTagCriterion
{
    /// <summary>
    /// Initializes a named tag criterion.
    /// </summary>
    /// <param name="name">The required name.</param>
    /// <param name="values">Optional values; null means any value on that name.</param>
    public NamedTagCriterion(TagName name, IEnumerable<string>? values = null)
    {
        Name = name;
        Values = values;
    }

    /// <summary>
    /// Gets the required name.
    /// </summary>
    /// <exception cref="InvalidNamedTagCriterion">The name is null or blank.</exception>
    public TagName Name
    {
        get;
        init
        {
            if (value is null || string.IsNullOrWhiteSpace(value.Value))
            {
                throw new InvalidNamedTagCriterion();
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets the allowed values, or null for any value on the name.
    /// </summary>
    /// <exception cref="InvalidNamedTagCriterion">The supplied set is empty or contains a null value.</exception>
    public IEnumerable<string>? Values
    {
        get;
        init
        {
            if (value is null)
            {
                field = null;
                return;
            }

            var snapshot = value.ToArray();
            if (snapshot.Length == 0 || snapshot.Any(item => item is null))
            {
                throw new InvalidNamedTagCriterion();
            }

            field = Array.AsReadOnly(snapshot);
        }
    }
}
