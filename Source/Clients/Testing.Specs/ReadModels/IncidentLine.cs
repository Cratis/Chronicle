// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line keyed by a <see cref="Severity"/> enum.
/// </summary>
/// <param name="Level">The severity level, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
public record IncidentLine(
    Severity Level,
    decimal Amount);
