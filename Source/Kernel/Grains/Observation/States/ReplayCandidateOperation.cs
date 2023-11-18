// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Operations;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayCandidateOperation"/>.
/// </summary>
public class ReplayCandidateOperation : Operation<ReplayCandidateRequest>, IReplayCandidateOperation
{
}
