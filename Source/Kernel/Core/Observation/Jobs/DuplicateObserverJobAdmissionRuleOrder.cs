// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// The exception that is thrown when two observer job admission rules share an order.
/// </summary>
/// <param name="order">The order the rules share.</param>
/// <param name="rules">The names of the rules sharing it.</param>
public class DuplicateObserverJobAdmissionRuleOrder(int order, IEnumerable<string> rules)
    : Exception($"Observer job admission rules {string.Join(", ", rules)} share order {order}. Every rule needs an order of its own, so the order they are asked in is never left to registration.");
