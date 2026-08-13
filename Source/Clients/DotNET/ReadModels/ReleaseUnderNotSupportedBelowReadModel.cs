// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a <see cref="Compliance.GDPR.SubjectFromAttribute">[SubjectFrom]</see> is declared below
/// the read model itself — on a member of a value object, a child element, or any other nested type.
/// </summary>
/// <remarks>
/// The declaration names a property of the read model, so it can only be read where the read model is: on
/// the read model's own properties. Honoring one further down would mean resolving a name against a type
/// that does not hold it. Reporting it is the point — a declaration that is quietly ignored puts the value
/// back into exactly the silent outcome it was written to avoid. Move the attribute up to the read model
/// property that holds the nested value; everything beneath it is released under the declared subject.
/// </remarks>
/// <param name="readModelType">The read model type being released.</param>
/// <param name="declaringType">The nested type carrying the declaration.</param>
/// <param name="propertyName">The name of the nested property carrying the declaration.</param>
public class ReleaseUnderNotSupportedBelowReadModel(Type readModelType, Type declaringType, string propertyName)
    : Exception($"[SubjectFrom] on '{declaringType.Name}.{propertyName}' is nested inside read model '{readModelType.Name}' and cannot be honored. Declare it on the property of '{readModelType.Name}' that holds the value instead — the whole value is released under the declared subject.")
{
    /// <summary>
    /// Gets the read model type being released.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;

    /// <summary>
    /// Gets the nested type carrying the declaration.
    /// </summary>
    public Type DeclaringType { get; } = declaringType;

    /// <summary>
    /// Gets the name of the nested property carrying the declaration.
    /// </summary>
    public string PropertyName { get; } = propertyName;
}
