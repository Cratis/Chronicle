// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when missing the on property expression when joining with specific event type.
/// </summary>
/// <param name="modelType">Type of model joining with event.</param>
/// <param name="eventType">Type of event that is being joined.</param>
public class MissingOnPropertyExpressionWhenJoiningWithEvent(Type modelType, Type eventType)
    : Exception($"Missing the on property expression for model of type '{modelType.FullName}' when joining with event of type '{eventType.FullName}'");
