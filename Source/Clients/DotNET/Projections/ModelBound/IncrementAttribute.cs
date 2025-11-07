// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property value should be incremented when an event occurs.
/// </summary>
/// <typeparam name="TEvent">The type of event that triggers the increment.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class IncrementAttribute<TEvent> : Attribute, IProjectionAnnotation, IIncrementAttribute;
