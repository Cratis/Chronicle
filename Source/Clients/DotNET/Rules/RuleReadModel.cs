// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Represents a read model for a business rule.
/// </summary>
/// <param name="ReadModelType">The type of the read model.</param>
/// <param name="ReadModelName">The name of the read model.</param>
public record RuleReadModel(Type ReadModelType, ReadModelName ReadModelName) : IHaveReadModel;
