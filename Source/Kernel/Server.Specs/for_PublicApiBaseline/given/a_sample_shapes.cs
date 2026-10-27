// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal class a_sample_shapes<T>
    where T : class
{
    /// <summary>
    /// The sample constant, including escaped line endings.
    /// </summary>
    public const string Label = "first\nsecond";

    /// <summary>
    /// The sample readonly field.
    /// </summary>
    public static readonly int Count = int.Parse("1", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets or sets a required value.
    /// </summary>
    public required T Value { get; set; }

    /// <summary>
    /// Gets or sets a nullable array of nullable elements.
    /// </summary>
    public string?[]? Items { get; set; }

    /// <summary>
    /// An event with inert accessors.
    /// </summary>
    public event Action? Changed
    {
        add { }
        remove { }
    }

    /// <summary>
    /// Exercises nested generic identities.
    /// </summary>
    /// <typeparam name="TItem">The nested argument.</typeparam>
    public class a_nested_shape<TItem>
        where TItem : struct;
}
