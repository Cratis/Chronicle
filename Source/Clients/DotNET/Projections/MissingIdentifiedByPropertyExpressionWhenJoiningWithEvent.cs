// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that is thrown when the on property expression is missing for the root projection.
/// </summary>
/// <param name="modelType">Type of model joining with event.</param>
/// <param name="eventType">Type of event that is being joined.</param>
public class MissingIdentifiedByPropertyExpressionWhenJoiningWithEvent(Type modelType, Type eventType)
    : Exception($"Missing the identified by property expression for model of type '{modelType.FullName}' when joining with event of type '{eventType.FullName}'. Use the `.IdentifiedBy()` method on the child builder.");
