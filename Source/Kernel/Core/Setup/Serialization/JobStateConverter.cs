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
/// Represents a <see cref="JsonConverter{T}"/> that can convert <see cref="JobState"/>.
/// </summary>
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
    /// <remarks>
    /// Job grains persist their own <see cref="JobState"/>-derived state type, so the converter must own those types
    /// too - exactly as MongoDB's <c>JobStateSerializationProvider</c> hands every <see cref="JobState"/> subclass to
    /// its serializer. Without this, System.Text.Json falls back to the default converter for a derived state, which
    /// writes the <see cref="JobState.Request"/> declared as the empty marker interface <c>IJobRequest</c> as an empty
    /// object and cannot read it back at all.
    /// </remarks>
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

        // Write only emits the request when there is one, and JobState.Request starts out unset, so a state persisted
        // before its request was assigned has no request to read back - and nothing to resolve a request type for.
        // Mirrors the MongoDB serializer's bookmark handling.
        if (nodeAsObject[nameof(JobState.Request).ToCamelCase()] is { } requestNode)
        {
            var jobRequestType = _jobTypes.Value.GetRequestClrTypeForOrThrow(jobStateResult.Type);
            var jobStateRequestProperty = typeToConvert.GetProperty(nameof(JobState.Request))!;
            jobStateRequestProperty.SetValue(jobStateResult, requestNode.Deserialize(jobRequestType, options));
        }

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
