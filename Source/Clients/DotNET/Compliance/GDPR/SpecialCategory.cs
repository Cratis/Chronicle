// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Defines the special categories of personal data under GDPR Article 9. A value declares at most one category.
/// </summary>
public enum SpecialCategory
{
    /// <summary>
    /// No special category is declared.
    /// </summary>
    None = 0,

    /// <summary>
    /// Personal data revealing racial or ethnic origin.
    /// </summary>
    RacialOrEthnicOrigin = 1,

    /// <summary>
    /// Personal data revealing political opinions.
    /// </summary>
    PoliticalOpinions = 2,

    /// <summary>
    /// Personal data revealing religious or philosophical beliefs.
    /// </summary>
    ReligiousOrPhilosophicalBeliefs = 3,

    /// <summary>
    /// Personal data revealing trade union membership.
    /// </summary>
    TradeUnionMembership = 4,

    /// <summary>
    /// Genetic data.
    /// </summary>
    Genetic = 5,

    /// <summary>
    /// Biometric data used to uniquely identify a natural person.
    /// </summary>
    Biometric = 6,

    /// <summary>
    /// Data concerning health.
    /// </summary>
    Health = 7,

    /// <summary>
    /// Data concerning a natural person's sex life or sexual orientation.
    /// </summary>
    SexLifeOrSexualOrientation = 8
}
