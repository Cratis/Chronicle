// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the add and subtract operations for one target type, resolved once so no per-event type switching is needed.
/// </summary>
/// <param name="Add">Adds the second value to the first and returns the result as the target type.</param>
/// <param name="Subtract">Subtracts the second value from the first and returns the result as the target type.</param>
internal sealed record NumericOperations(Func<object, object, object> Add, Func<object, object, object> Subtract);
