// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Expressions.Keys;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

public partial class KeyExpressionResolvers;

internal static partial class KeyExpressionResolversLogging
{
    [LoggerMessage(LogLevel.Error, "Unsupported key expression: {Expression}")]
    internal static partial void UnsupportedKeyExpression(this ILogger<KeyExpressionResolvers> logger, string expression);
}
