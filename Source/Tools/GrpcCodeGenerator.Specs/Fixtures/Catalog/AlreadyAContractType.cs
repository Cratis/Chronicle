// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SharedTypeCatalog;

/// <summary>
/// Stands in for a type that already lives under the contracts namespace - never a candidate, because it is
/// already what shared types get mirrored into.
/// </summary>
public class AlreadyAContractType
{
    /// <summary>Gets or sets a value.</summary>
    public string Value { get; set; } = string.Empty;
}
