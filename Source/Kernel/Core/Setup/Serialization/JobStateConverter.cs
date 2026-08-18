// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Strings;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverter{T}"/> that can convert <see cref="JobState"/> and any type deriving from it.
/// </summary>
/// <remarks>
/// The derived job states are what actually reach storage - a job grain persists its own state type, not the base
/// <see cref="JobState"/>. <see cref="JobState.Request"/> is declared as the empty marker interface
/// <see cref="IJobRequest"/>, so the default converter writes it as <c>{}</c> and the request is lost. Converting
/// derived types here as well keeps the request round-tripping for every job state that is stored as JSON.
/// </remarks>
public sealed class JobStateConverter : JsonConverter<JobState>
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
    public override bool CanConvert(Type typeToConvert) => typeof(JobState).IsAssignableFrom(typeToConvert);

    /// <inheritdoc/>
    public override JobState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonElement.ParseValue(ref reader);
        if (node.ValueKind != JsonValueKind.Object)
        {
            return default!;
        }
        var jobStateResult = (JobState)Activator.CreateInstance(typeToConvert)!;
        var nodeAsObject = JsonObject.Create(node);
        jobStateResult.Type = new(nodeAsObject![nameof(JobState.Type).ToCamelCase()]!.ToString());
        var jobRequestType = _jobTypes.Value.GetRequestClrTypeForOrThrow(jobStateResult.Type);

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
    public override void Write(Utf8JsonWriter writer, JobState value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var property in value.GetType().GetProperties())
        {
            var propertyValue = property.GetValue(value);
            if (propertyValue is null)
            {
                continue;
            }

            var propertyName = property.Name.ToCamelCase();
            writer.WritePropertyName(propertyName);
            JsonSerializer.Serialize(writer, propertyValue, options);
        }

        writer.WriteEndObject();
    }
}
