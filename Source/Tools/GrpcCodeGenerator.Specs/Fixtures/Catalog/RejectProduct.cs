// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace TestAssembly.Catalog;

/// <summary>
/// A command that can reject itself by returning a validation result.
/// </summary>
/// <param name="Name">The product name.</param>
[Command]
public record RejectProduct(string Name)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>A validation error for an empty name, otherwise null.</returns>
    public ValidationResult? Handle() =>
        string.IsNullOrEmpty(Name) ? ValidationResult.Error("Name is required.", [nameof(Name)]) : null;
}
