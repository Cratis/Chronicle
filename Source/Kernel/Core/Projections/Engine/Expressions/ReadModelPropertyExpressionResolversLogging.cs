// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public partial class ReadModelPropertyExpressionResolvers;

internal static partial class ReadModelPropertyExpressionResolversLogging
{
    [LoggerMessage(LogLevel.Error, "Unsupported read model property expression: {Expression} for target property: {TargetProperty}")]
    internal static partial void UnsupportedReadModelPropertyExpression(this ILogger<ReadModelPropertyExpressionResolvers> logger, string expression, PropertyPath targetProperty);
}
