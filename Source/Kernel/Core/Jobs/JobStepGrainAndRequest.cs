// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents a <see cref="IJobStep"/> grain and its request.
/// </summary>
/// <param name="Grain">The <see cref="IJobStep"/> grain.</param>
/// <param name="Request">The request object associated.</param>
public record JobStepGrainAndRequest(IJobStep Grain, object Request);
