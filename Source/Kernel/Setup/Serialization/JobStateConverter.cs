// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Json;
using Cratis.Strings;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that can convert <see cref="JobState"/>.
/// </summary>
public class JobStateConverter : JsonConverter<JobState>
{
    readonly Lazy<IJobTypes> _jobTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStateConverter"/> class.
    /// </summary>
    /// <param name="jobTypes"><see cref="IJobTypes"/>.</param>
    public JobStateConverter(IJobTypes jobTypes)
    {
        _jobTypes = new(jobTypes);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStateConverter"/> class.
    /// </summary>
    public JobStateConverter()
    {
        _jobTypes = new(() => JobTypes.Instance);
    }

    /// <inheritdoc/>
    public override JobState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonElement.ParseValue(ref reader);
        if (node.ValueKind != JsonValueKind.Object)
        {
            return default!;
        }
        var jobStateResult = new JobState();
        var nodeAsObject = JsonObject.Create(node);
        jobStateResult.Type = new(nodeAsObject![nameof(JobState.Type).ToCamelCase()]!.ToString());
        var jobRequestType = _jobTypes.Value.GetRequestClrTypeFor(jobStateResult.Type).Match(type => type, error => throw new UnknownClrTypeForJobType(jobStateResult.Type));

        foreach (var (field, value) in nodeAsObject)
        {
            if (field == nameof(JobState.Request).ToCamelCase())
            {
                continue;
            }

            var propertyName = field.ToPascalCase();
            var jobStatePropertyInfo = typeToConvert.GetProperty(propertyName);
            if (jobStatePropertyInfo?.SetMethod is null)
            {
                continue;
            }

            var deserializedValue = value.Deserialize(jobStatePropertyInfo.PropertyType, options);
            jobStatePropertyInfo.SetValue(jobStateResult, deserializedValue);
        }
        var jobStateRequestProperty = typeToConvert.GetProperty(nameof(JobState.Request))!;
        var jobRequest = nodeAsObject[nameof(JobState.Request).ToCamelCase()]!.Deserialize(jobRequestType, options);
        jobStateRequestProperty.SetValue(jobStateResult, jobRequest);

        return jobStateResult;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, JobState value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, Globals.JsonSerializerOptions);
}
