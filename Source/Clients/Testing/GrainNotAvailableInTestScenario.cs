// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Exception that gets thrown when a grain is asked for in a scenario that has no silo behind it.
/// </summary>
/// <remarks>
/// The in-process scenarios run the projection and reducer engines directly, with no cluster - so nothing can
/// hand out a grain reference. Failing by name rather than as a bare framework exception says which grain was
/// wanted, which is the part a reader needs: the answer is almost always to assert on the scenario's own
/// surface instead of reaching for the one a running system would have.
/// </remarks>
/// <param name="description">Description of what was asked for.</param>
public class GrainNotAvailableInTestScenario(string description)
    : Exception($"{description} is not available in test scenarios - the in-process scenarios run without a silo.");
