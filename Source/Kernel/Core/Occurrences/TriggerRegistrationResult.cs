// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Reports whether every trigger payload contract was registered.
/// </summary>
/// <param name="Registered">Whether the registration completed.</param>
/// <param name="Errors">The validation or concurrent registration errors.</param>
public record TriggerRegistrationResult(bool Registered, IEnumerable<string> Errors);
