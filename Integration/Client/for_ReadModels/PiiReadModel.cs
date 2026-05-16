// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Integration.for_ReadModels;

/// <summary>
/// Read model with a PII property used in integration tests.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="SocialSecurityNumber">The social security number (PII).</param>
public record PiiReadModel(string Name, [property: PII] string SocialSecurityNumber);
