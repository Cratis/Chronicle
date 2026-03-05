// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public partial class EventValueProviderExpressionResolvers;

internal static partial class EventValueProviderExpressionResolversLogging
{
    [LoggerMessage(LogLevel.Error, "Unsupported event value expression: {Expression} for target property type: {TargetPropertyType}")]
    internal static partial void UnsupportedEventValueExpression(this ILogger<EventValueProviderExpressionResolvers> logger, string expression, JsonObjectType targetPropertyType);
}
