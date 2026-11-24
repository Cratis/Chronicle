// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents an attribute that can be used to mark classes or properties to indicate the information kept is PII according to the definition of GDPR.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PIIAttribute"/> class.
/// </remarks>
/// <param name="details">Optional details - default value is empty string.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PIIAttribute(string details = "") : Attribute
{
    /// <summary>
    /// Gets the details as to why or to what purpose/extent the type or property marked is classified as PII.
    /// </summary>
    public string Details { get; } = details;

    /// <summary>
    /// Gets the GDPR Article 9 special category, if any. This classifies the value only and adds no protection.
    /// </summary>
    public SpecialCategory SpecialCategory { get; init; }

    /// <summary>
    /// Gets whether the value relates to criminal convictions and offenses under GDPR Article 10.
    /// This classifies the value only and adds no protection.
    /// </summary>
    public bool Criminal { get; init; }

    /// <summary>
    /// Gets whether the personal value is also an operational secret. This classifies the value only and adds no protection;
    /// subject encryption already covers it. Do not add <see cref="ProtectedValues.EncryptedAttribute"/>; Chronicle rejects combining it with PII.
    /// </summary>
    public bool OperationalSecret { get; init; }
}
