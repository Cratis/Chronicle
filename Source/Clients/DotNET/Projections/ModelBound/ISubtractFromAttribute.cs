// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines an attribute that indicates that a property value should be subtracted from an event property.
/// </summary>
public interface ISubtractFromAttribute : IEventBoundAttribute, ICanMapToEventProperty;
