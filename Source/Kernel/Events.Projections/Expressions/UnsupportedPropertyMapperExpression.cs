// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Exception that gets thrown when an event value expression is not supported.
    /// </summary>
    public class UnsupportedPropertyMapperExpression : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedPropertyMapperExpression"/> class.
        /// </summary>
        /// <param name="expression">The unsupported expression.</param>
        public UnsupportedPropertyMapperExpression(string expression) : base($"Couldn't find an property mapper for the expression '{expression}'") { }
    }
}
