// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Models;

/// <summary>
/// Attribute that can be adorned a model type as metadata to indicate the actual name of the model.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModelNameAttribute"/> class.
/// </remarks>
/// <param name="name">Name of the model.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ModelNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the model.
    /// </summary>
    public string Name { get; } = name;
}
