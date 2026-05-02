// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate which event clears (sets to null) a nested single-object property.
/// </summary>
/// <typeparam name="TEvent">The type of event that clears the nested object.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ClearWithAttribute<TEvent> : Attribute, IProjectionAnnotation, IClearWithAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);
}
