// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Json;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents the <see cref="JsonConverter{T}"/> that can convert <see cref="JobState"/>.
/// </summary>
public class JobStateJsonConverter : TypeWithObjectPropertiesJsonConverter<JobState>
{
    /// <inheritdoc/>
    protected override IEnumerable<string> ObjectProperties => [nameof(JobState.Request)];
}
