// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate which event clears (sets to null) a read model member.
/// </summary>
/// <typeparam name="TEvent">The type of event that clears the member.</typeparam>
/// <remarks>
/// <para>
/// On a scalar property or record parameter this clears that member: the projection writes null to it every time
/// the event is observed, replay included. The member has to be able to hold null - a non-nullable member is
/// rejected when the projection is built, because writing its type default would be a different fact than
/// "no value" and is what <see cref="SetValueAttribute{TEvent}"/> is for.
/// </para>
/// <para>
/// On a <see cref="NestedAttribute"/> single-object property, and applied to the nested type itself, this clears
/// the whole nested object back to null.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ClearWithAttribute<TEvent> : Attribute, IProjectionAnnotation, IClearWithAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);
}
