// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Events.Projections.Expressions
{
    /// <summary>
    /// Represents a <see cref="IPropertyMapperExpressionResolver"/> for adding value on a model with the value for a property on the content of an <see cref="Event"/>.
    /// </summary>
    public class AddExpressionResolver : IPropertyMapperExpressionResolver
    {
        static Regex _regularExpression = new Regex("\\$add\\(([A-Za-z.]*)\\)", RegexOptions.Compiled);

        /// <inheritdoc/>
        public bool CanResolve(string targetProperty, string expression) => _regularExpression.Match(expression).Success;

        /// <inheritdoc/>
        public PropertyMapper Resolve(string targetProperty, string expression)
        {
            var match = _regularExpression.Match(expression);
            return PropertyMappers.AddWithEventValueProvider(targetProperty, EventValueProviders.FromEventContent(match.Groups[1].Value));
        }
    }
}
