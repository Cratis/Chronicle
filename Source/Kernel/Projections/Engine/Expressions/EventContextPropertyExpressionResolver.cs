// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Represents a <see cref="IPropertyMapperExpressionResolver"/> for resolving value from <see cref="EventSourceId"/> of the <see cref="AppendedEvent"/>.
/// </summary>
public class EventContextPropertyExpressionResolver : IPropertyMapperExpressionResolver
{
    static readonly Regex _regularExpression = new("\\$eventContext\\((?<property>[A-Za-z.]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, string expression)
    {
        var match = _regularExpression.Match(expression);
        return PropertyMappers.FromEventValueProvider(targetProperty, EventValueProviders.FromEventContext(match.Groups["property"].Value));
    }
}
