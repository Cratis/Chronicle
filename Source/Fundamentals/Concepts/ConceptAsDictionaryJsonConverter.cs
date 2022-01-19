// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Aksio.Cratis.Reflection;
using Newtonsoft.Json;

namespace Aksio.Cratis.Concepts
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can serialize and deserialize a <see cref="IDictionary{TKey, TValue}">dictionary</see> of <see cref="ConceptAs{T}"/>.
    /// </summary>
    public class ConceptAsDictionaryJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (objectType.HasInterface(typeof(IDictionary<,>)) && objectType.GetType().IsGenericType)
            {
                var keyType = objectType.GetType().GetGenericArguments()[0].GetType().BaseType!;
                if (keyType.GetType().IsGenericType)
                {
                    var genericArgumentType = keyType.GetType().GetGenericArguments()[0];
                    return typeof(ConceptAs<>).MakeGenericType(genericArgumentType).GetType().IsAssignableFrom(keyType);
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return default;
            }

            var keyType = objectType.GetType().GetGenericArguments()[0]!;
            var keyValueType = keyType
                .GetType()!
                .BaseType!
                .GetType()
                .GetGenericArguments()[0];

            var valueType = objectType.GetType().GetGenericArguments()[1];
            var intermediateDictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
            var intermediateDictionary = (Activator.CreateInstance(intermediateDictionaryType) as IDictionary)!;
            serializer.Populate(reader, intermediateDictionary);

            var valueProperty = keyType.GetType().GetProperty("Value")!;
            var finalDictionary = (Activator.CreateInstance(objectType) as IDictionary)!;
            foreach (DictionaryEntry pair in intermediateDictionary)
            {
                object value;
                if (keyValueType == typeof(Guid))
                {
                    value = Guid.Parse(pair.Key.ToString()!);
                }
                else
                {
                    value = Convert.ChangeType(pair.Key, keyValueType, null);
                }

                var key = Activator.CreateInstance(keyType)!;
                valueProperty.SetValue(key, value, null);
                finalDictionary.Add(key, pair.Value);
            }
            return finalDictionary;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var dictionary = value as IDictionary;
            if (dictionary == default)
            {
                return;
            }

            var objectType = dictionary.GetType();
            var keyType = objectType.GetType().GetGenericArguments()[0];
            var valueType = objectType.GetType().GetGenericArguments()[1];
            var intermediateDictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
            var intermediateDictionary = (Activator.CreateInstance(intermediateDictionaryType) as IDictionary)!;
            var valueProperty = keyType.GetType().GetProperty("Value")!;

            foreach (DictionaryEntry pair in dictionary)
            {
                var keyValue = valueProperty.GetValue(pair.Key, null)!.ToString()!;
                intermediateDictionary[keyValue] = pair.Value;
            }

            writer.WriteValue(intermediateDictionary);
        }
    }
}
