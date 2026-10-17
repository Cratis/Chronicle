// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// The exception that is thrown when a scenario attempts to move its clock backwards.
/// </summary>
public sealed class ScenarioTimeCannotMoveBackwards() : Exception("Scenario time cannot move backwards.");
