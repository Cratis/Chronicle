// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.RegularExpressions;
using Cratis.Events;
using Cratis.Properties;
using Cratis.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ModelProperties;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for adding value on a model with the value for a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CountExpressionResolver"/> class.
/// </remarks>
/// <param name="typeFormats"><see cref="ITypeFormats"/> to use for correct type conversion.</param>
public class CountExpressionResolver(ITypeFormats typeFormats) : IModelPropertyExpressionResolver
{
    static readonly Regex _regularExpression = new("\\$count\\(\\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    readonly ITypeFormats _typeFormats = typeFormats;

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _regularExpression.Match(expression).Success;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression) => PropertyMappers.Count(_typeFormats, targetProperty, targetPropertySchema);
}
