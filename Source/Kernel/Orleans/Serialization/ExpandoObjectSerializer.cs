// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Dynamic;
using Orleans.Serialization;

namespace Aksio.Cratis.Kernel.Orleans.Serialization;

/// <summary>
/// Represents a <see cref="IExternalSerializer"/> for <see cref="ExpandoObject"/>.
/// </summary>
public class ExpandoObjectSerializer : IExternalSerializer
{
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandoObjectSerializer"/> class.
    /// </summary>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> to use for serializing and deserializing.</param>
    public ExpandoObjectSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public object DeepCopy(object source, ICopyContext context) => (source as ExpandoObject)!.Clone();

    /// <inheritdoc/>
    public object Deserialize(Type expectedType, IDeserializationContext context) => JsonSerializer.Deserialize<ExpandoObject>(context.StreamReader.ReadString(), _jsonSerializerOptions)!;

    /// <inheritdoc/>
    public bool IsSupportedType(Type itemType) => itemType == typeof(ExpandoObject);

    /// <inheritdoc/>
    public void Serialize(object item, ISerializationContext context, Type expectedType) => context.StreamWriter.Write(JsonSerializer.Serialize(item, _jsonSerializerOptions));
}
