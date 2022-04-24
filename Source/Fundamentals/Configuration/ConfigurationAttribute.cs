// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;
#pragma warning disable AS0003  // Allow sealed attribute

/// <summary>
/// Attribute used to adorn configuration objects.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConfigurationAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the configuration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether or not the file is optional.
    /// </summary>
    public bool Optional { get; }

    /// <summary>
    /// Check whether or not the Name is set.
    /// </summary>
    public bool NameSet => !string.IsNullOrEmpty(Name);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationAttribute"/> class.
    /// </summary>
    /// <param name="name">Optional name of the configuration.</param>
    /// <param name="optional">Whether or not the file is optional - default = false.</param>
    public ConfigurationAttribute(string name = "", bool optional = false)
    {
        Name = name;
        Optional = optional;
    }
}
