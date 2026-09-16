// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace TestAssembly.Catalog;

/// <summary>
/// Exercises a parameterless command with writable body properties.
/// </summary>
[Command]
public record SelectReceipt
{
    /// <summary>
    /// Gets whether to include a receipt.
    /// </summary>
    public bool IncludeReceipt { get; init; }

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle() { }
}
