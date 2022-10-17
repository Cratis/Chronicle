// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IEventValueProviderExpressionResolver"/> for resolving a composite value from the event content.
/// </summary>
public class CompositeFromEventContextValueProviderExpressionResolver : IEventValueProviderExpressionResolver
{
    static readonly Regex _regularExpression = new("\\$compositeFromContext\\((?<properties>.*?)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <inheritdoc/>
    public bool CanResolve(string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression)
    {
        var match = _regularExpression.Match(expression);
        var properties = match.Groups["properties"].Value;
        var propertyPaths = properties.Split(',').Select(_ => (PropertyPath)_.Trim());
        return EventValueProviders.EventContentComposite(propertyPaths);
    }
}
