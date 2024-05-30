// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IComplianceMetadataResolver"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ComplianceMetadataResolver"/>.
/// </remarks>
/// <param name="typeProviders">Type providers.</param>
/// <param name="propertyProviders">Property providers.</param>
public class ComplianceMetadataResolver(
    IInstancesOf<ICanProvideComplianceMetadataForType> typeProviders,
    IInstancesOf<ICanProvideComplianceMetadataForProperty> propertyProviders) : IComplianceMetadataResolver
{
    readonly IEnumerable<ICanProvideComplianceMetadataForType> _typeProviders = [..typeProviders];
    readonly IEnumerable<ICanProvideComplianceMetadataForProperty> _propertyProviders = [..propertyProviders];

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
