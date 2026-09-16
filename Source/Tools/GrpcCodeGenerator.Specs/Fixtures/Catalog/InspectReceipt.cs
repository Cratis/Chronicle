// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands.ModelBound;

namespace TestAssembly.Catalog;

/// <summary>
/// Exercises properties that cannot be assigned from a request.
/// </summary>
[Command]
public record InspectReceipt
{
    /// <summary>
    /// Gets or sets process-wide state, not command input.
    /// </summary>
    public static bool Shared { get; set; }

    /// <summary>
    /// Gets a read-only value.
    /// </summary>
    public bool ReadOnly => true;

    /// <summary>
    /// Gets a value with a private setter.
    /// </summary>
    public bool PrivateSetter { get; private set; }

    /// <summary>
    /// Sets a value with a private getter.
    /// </summary>
    [SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "The fixture deliberately exercises exclusion of a property with a private getter.")]
    public bool PrivateGetter { private get; set; }

    bool PrivateProperty { get; set; }

    /// <summary>
    /// Gets or sets a value by index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The value.</returns>
    public bool this[int index]
    {
        get => index > 0;
        set => PrivateProperty = value;
    }

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle() => PrivateSetter = PrivateGetter || PrivateProperty;
}
