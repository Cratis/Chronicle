// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// One way the current grain contracts fail to serve the baseline.
/// </summary>
/// <param name="Kind">The <see cref="ContractBreakKind"/>.</param>
/// <param name="Subject">The interface or grain class the break is in, by its CLR name.</param>
/// <param name="Description">What changed, for a reader.</param>
public record ContractBreak(ContractBreakKind Kind, string Subject, string Description);
