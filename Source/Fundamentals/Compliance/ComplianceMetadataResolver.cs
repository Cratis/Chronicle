// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Compliance
{
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
        public bool HasMetadata(Type type) => _typeProviders.Any(_ => _.CanProvide(type));

        /// <inheritdoc/>
        public bool HasMetadata(PropertyInfo property) => _propertyProviders.Any(_ => _.CanProvide(property));

        /// <inheritdoc/>
        public ComplianceMetadata GetMetadataFor(Type type)
        {
            ThrowIfNoComplianceMetadataForType(type);
            return _typeProviders.Single(_ => _.CanProvide(type)).Provide(type);
        }

        /// <inheritdoc/>
        public ComplianceMetadata GetMetadataFor(PropertyInfo property)
        {
            ThrowIfNoComplianceMetadataForProperty(property);
            return _propertyProviders.Single(_ => _.CanProvide(property)).Provide(property);
        }

        void ThrowIfNoComplianceMetadataForType(Type type)
        {
            if (!HasMetadata(type))
            {
                throw new NoComplianceMetadataForType(type);
            }
        }

        void ThrowIfNoComplianceMetadataForProperty(PropertyInfo property)
        {
            if (!HasMetadata(property))
            {
                throw new NoComplianceMetadataForProperty(property);
            }
        }
    }
}
