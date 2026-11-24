// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Orleans.Serialization.Cloning;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Hands the caller the instance itself, instead of copying it through JSON, for job states.
/// </summary>
/// <remarks>
/// Orleans copies every argument and response of a call within a silo so neither side can change what the other holds. A
/// <see cref="JobState"/> is a state read from storage for the one call and not kept by the grain afterwards, so there is
/// nothing to protect. Copying it anyway cost a JSON round trip per call, megabytes each for a job state that carries the
/// plan of thousands of steps. Between silos it is still serialized.
/// The kernel's own immutable records are marked <see cref="ImmutableAttribute"/> where they are declared, which makes Orleans hand over the
/// reference without a copier. A <see cref="JobState"/> belongs to Cratis.Choreography, so it cannot carry the attribute.
/// </remarks>
public sealed class SharedInstanceCopier : IGeneralizedCopier
{
    /// <inheritdoc/>
    public bool IsSupportedType(Type type) => typeof(JobState).IsAssignableFrom(type);

    /// <inheritdoc/>
    public object? DeepCopy(object? input, CopyContext context) => input;
}
