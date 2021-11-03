// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a <see cref="IPropertyExpressionBuilder"/> for expressing the parent key property in a parent->child relationship.
    /// </summary>
    public class ParentKeyPropertyExpressionBuilder : IPropertyExpressionBuilder
    {
        readonly string _propertyName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentKeyPropertyExpressionBuilder"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property on event used for parent key.</param>
        public ParentKeyPropertyExpressionBuilder(string propertyName)
        {
            _propertyName = propertyName;
        }

        /// <inheritdoc/>
        public string TargetProperty => "$parentKey";

        /// <inheritdoc/>
        public string Build() => _propertyName;
    }
}
