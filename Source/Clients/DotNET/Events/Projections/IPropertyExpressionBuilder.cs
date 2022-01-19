// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a builder of a property expressions.
    /// </summary>
    public interface IPropertyExpressionBuilder
    {
        /// <summary>
        /// Gets the target property.
        /// </summary>
        PropertyPath TargetProperty { get; }

        /// <summary>
        /// Builds the expression.
        /// </summary>
        /// <returns>The expression built.</returns>
        string Build();
    }
}
