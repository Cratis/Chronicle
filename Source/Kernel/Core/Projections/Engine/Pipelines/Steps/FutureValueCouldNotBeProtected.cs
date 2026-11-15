// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Exception thrown when a future's protected value cannot be located in the protected snapshot.
/// </summary>
/// <param name="propertyPath">The path of the value.</param>
/// <param name="sequenceNumber">The future event sequence number.</param>
public class FutureValueCouldNotBeProtected(PropertyPath propertyPath, EventSequenceNumber sequenceNumber)
    : Exception($"Future value at '{propertyPath}' for event sequence number '{sequenceNumber}' could not be protected");
