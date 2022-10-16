// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions.EventValues;

/// <summary>
/// Represents a <see cref="IEventValueProviderExpressionResolver"/> for resolving a composite value from the event content.
/// </summary>
public class CompositeValueProviderExpressionResolver : IEventValueProviderExpressionResolver
{
    static readonly Regex _regularExpression = new("\\$composite\\((?<property>[A-Za-z.]*)\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <inheritdoc/>
    public bool CanResolve(string expression) => throw new NotImplementedException();

    /// <inheritdoc/>
    public ValueProvider<AppendedEvent> Resolve(string expression) => throw new NotImplementedException();
}

