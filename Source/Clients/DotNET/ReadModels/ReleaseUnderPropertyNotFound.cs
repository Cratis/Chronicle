// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a <see cref="Compliance.GDPR.ReleaseUnderAttribute"/> names a property
/// that does not exist on the read model.
/// </summary>
/// <remarks>
/// The declaration exists to make the outcome of the release pass explicit, so a name that resolves to
/// nothing has to fail rather than fall back — falling back would put the value straight back into one of
/// the two silent outcomes the declaration was written to avoid.
/// </remarks>
/// <param name="readModelType">The read model type carrying the declaration.</param>
/// <param name="propertyName">The name of the property carrying the declaration.</param>
/// <param name="subjectPropertyName">The name the declaration points at.</param>
public class ReleaseUnderPropertyNotFound(Type readModelType, string propertyName, string subjectPropertyName)
    : Exception($"[ReleaseUnder(\"{subjectPropertyName}\")] on '{readModelType.Name}.{propertyName}' names a property that does not exist on '{readModelType.Name}'. Use nameof() with a public instance property of the read model holding the subject to release under.")
{
    /// <summary>
    /// Gets the read model type carrying the declaration.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;

    /// <summary>
    /// Gets the name of the property carrying the declaration.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets the name the declaration points at.
    /// </summary>
    public string SubjectPropertyName { get; } = subjectPropertyName;
}
