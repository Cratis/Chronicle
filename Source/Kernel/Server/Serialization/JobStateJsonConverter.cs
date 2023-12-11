// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Server.Serialization;

/// <summary>
/// Represents the <see cref="JsonConverter{T}"/> that can convert <see cref="JobState"/>.
/// </summary>
public class JobStateJsonConverter : TypeWithObjectPropertiesJsonConverter<JobState>
{
    /// <inheritdoc/>
    protected override IEnumerable<string> ObjectProperties => new[]
    {
        nameof(JobState.Request)
    };
}
