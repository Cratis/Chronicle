// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IComplianceMetadataResolver"/>.
/// </summary>
public class ComplianceMetadataResolver : IComplianceMetadataResolver
{
    readonly IEnumerable<ICanProvideComplianceMetadataForType> _typeProviders;
    readonly IEnumerable<ICanProvideComplianceMetadataForProperty> _propertyProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceMetadataResolver"/>.
    /// </summary>
    /// <param name="typeProviders">Type providers.</param>
    /// <param name="propertyProviders">Property providers.</param>
    public ComplianceMetadataResolver(
        IInstancesOf<ICanProvideComplianceMetadataForType> typeProviders,
        IInstancesOf<ICanProvideComplianceMetadataForProperty> propertyProviders)
    {
        _typeProviders = typeProviders.ToArray();
        _propertyProviders = propertyProviders.ToArray();
    }

    /// <inheritdoc/>
    public bool HasMetadataFor(Type type) => _typeProviders.Any(_ => _.CanProvide(type));

    /// <inheritdoc/>
    public bool HasMetadataFor(PropertyInfo property) => _propertyProviders.Any(_ => _.CanProvide(property));

    /// <inheritdoc/>
    public IEnumerable<ComplianceMetadata> GetMetadataFor(Type type)
    {
        ThrowIfNoComplianceMetadataForType(type);
        return _typeProviders
            .Where(_ => _.CanProvide(type))
            .Select(_ => _.Provide(type))
            .ToArray();
    }

    /// <inheritdoc/>
    public IEnumerable<ComplianceMetadata> GetMetadataFor(PropertyInfo property)
    {
        ThrowIfNoComplianceMetadataForProperty(property);
        return _propertyProviders
            .Where(_ => _.CanProvide(property))
            .Select(_ => _.Provide(property))
            .ToArray();
    }

    void ThrowIfNoComplianceMetadataForType(Type type)
    {
        if (!HasMetadataFor(type))
        {
            throw new NoComplianceMetadataForType(type);
        }
    }

    void ThrowIfNoComplianceMetadataForProperty(PropertyInfo property)
    {
        if (!HasMetadataFor(property))
        {
            throw new NoComplianceMetadataForProperty(property);
        }
    }
}
