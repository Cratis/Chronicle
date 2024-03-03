// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Storage.Jobs;

namespace Aksio.Cratis.Kernel.Server.Serialization;

/// <summary>
/// Represents the <see cref="JsonConverter{T}"/> that can convert <see cref="JobStepState"/>.
/// </summary>
public class JobStepStateJsonConverter : TypeWithObjectPropertiesJsonConverter<JobStepState>
{
    /// <inheritdoc/>
    protected override IEnumerable<string> ObjectProperties => new[]
    {
        nameof(JobStepState.Request)
    };
}
