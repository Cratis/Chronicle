// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;

public class PrefixNamingPolicy : INamingPolicy
{
    public JsonNamingPolicy JsonPropertyNamingPolicy { get; } = new PrefixJsonNamingPolicy();
    public string GetReadModelName(Type readModelType) => readModelType.Name;
    public string GetPropertyName(string name) => JsonPropertyNamingPolicy.ConvertName(name);
}
