// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Models;

/// <summary>
/// Attribute that can be adorned a model type as metadata to indicate the actual name of the model.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ModelNameAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the model.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelNameAttribute"/> class.
    /// </summary>
    /// <param name="name">Name of the model.</param>
    public ModelNameAttribute(string name)
    {
        Name = name;
    }
}
