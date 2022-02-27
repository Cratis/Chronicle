// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Attribute used to adorn properties to use a specific <see cref="IConfigurationValueResolver"/> for resolving its value.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ConfigurationValueResolverAttribute : Attribute
{
    /// <summary>
    /// Gets the <see cref="IConfigurationValueResolver"/>.
    /// </summary>
    public Type ResolverType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValueResolverAttribute"/> class.
    /// </summary>
    /// <param name="resolverType">Type of <see cref="IConfigurationValueResolver"/> to use.</param>
    public ConfigurationValueResolverAttribute(Type resolverType)
    {
        if (!resolverType.IsAssignableTo(typeof(IConfigurationValueResolver)))
        {
            throw new InvalidResolverType(resolverType);
        }
        ResolverType = resolverType;
    }
}
