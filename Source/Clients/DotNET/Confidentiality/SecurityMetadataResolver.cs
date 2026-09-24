// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Represents an implementation of <see cref="ISecurityMetadataResolver"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SecurityMetadataResolver"/>.
/// </remarks>
/// <param name="typeProviders">Type providers.</param>
/// <param name="propertyProviders">Property providers.</param>
public class SecurityMetadataResolver(
    IInstancesOf<ICanProvideSecurityMetadataForType> typeProviders,
    IInstancesOf<ICanProvideSecurityMetadataForProperty> propertyProviders) : ISecurityMetadataResolver
{
    readonly IEnumerable<ICanProvideSecurityMetadataForType> _typeProviders = [.. typeProviders.Where(_ => _ is not null)];
    readonly IEnumerable<ICanProvideSecurityMetadataForProperty> _propertyProviders = [.. propertyProviders.Where(_ => _ is not null)];

    /// <inheritdoc/>
    public bool HasMetadataFor(Type type) => _typeProviders.Any(_ => _.CanProvide(type));

    /// <inheritdoc/>
    public bool HasMetadataFor(PropertyInfo property) => _propertyProviders.Any(_ => _.CanProvide(property));

    /// <inheritdoc/>
    public IEnumerable<SecurityMetadata> GetMetadataFor(Type type)
    {
        ThrowIfNoSecurityMetadataForType(type);
        return _typeProviders
            .Where(_ => _.CanProvide(type))
            .Select(_ => _.Provide(type))
            .ToArray();
    }

    /// <inheritdoc/>
    public IEnumerable<SecurityMetadata> GetMetadataFor(PropertyInfo property)
    {
        ThrowIfNoSecurityMetadataForProperty(property);
        return _propertyProviders
            .Where(_ => _.CanProvide(property))
            .Select(_ => _.Provide(property))
            .ToArray();
    }

    void ThrowIfNoSecurityMetadataForType(Type type)
    {
        if (!HasMetadataFor(type))
        {
            throw new NoSecurityMetadataForType(type);
        }
    }

    void ThrowIfNoSecurityMetadataForProperty(PropertyInfo property)
    {
        if (!HasMetadataFor(property))
        {
            throw new NoSecurityMetadataForProperty(property);
        }
    }
}
