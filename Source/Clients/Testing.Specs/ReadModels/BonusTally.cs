// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A read model holding a running count that includes a bonus supplied by an injected service, used to verify
/// reducer dependency injection through ReadModelScenario.Services.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Count">The running count including bonuses.</param>
public record BonusTally(Guid Id, int Count);
