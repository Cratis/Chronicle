// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;
namespace Cratis.Chronicle.Json;

/// <summary>
/// Extension methods for <see cref="object"/>.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Gets the object as <typeparamref name="TResult"/>, deserializing json if necessary.
    /// </summary>
    /// <param name="item">The object.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>The result.</returns>
    public static TResult? DeserializeIfNecessary<TResult>(this object item, JsonSerializerOptions? jsonSerializerOptions = null)
        where TResult : class
    {
        jsonSerializerOptions ??= Globals.JsonSerializerOptions;
        switch (item)
        {
            case null:
                return null;
            case TResult partialResult:
                return partialResult;
            case JsonElement jsonResult:
                return jsonResult.Deserialize<TResult>(jsonSerializerOptions);
            default:
                var json = JsonSerializer.Serialize(item, jsonSerializerOptions);
                return JsonSerializer.Deserialize<TResult>(json, jsonSerializerOptions);
        }
    }
}
