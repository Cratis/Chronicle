// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// The exception that is thrown when a unique-constraint number is NaN or infinity.
/// </summary>
internal class NonFiniteUniqueConstraintNumber() : Exception("Unique-constraint encoding v2 requires finite numbers.");
