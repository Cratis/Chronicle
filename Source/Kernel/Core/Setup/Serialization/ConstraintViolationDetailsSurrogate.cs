// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries <see cref="ConstraintViolationDetails"/> between grains as the dictionary it is.
/// </summary>
/// <param name="Details">The details by name.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.ConstraintViolationDetailsSurrogate")]
public readonly record struct ConstraintViolationDetailsSurrogate([property: Id(0)] IDictionary<string, string> Details);
