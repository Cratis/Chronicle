// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// The installation was activated with a license token.
/// </summary>
/// <param name="Token">The license token - shared across the whole installation.</param>
[EventType]
public record LicenseActivated(InstallationLicenseToken Token);
