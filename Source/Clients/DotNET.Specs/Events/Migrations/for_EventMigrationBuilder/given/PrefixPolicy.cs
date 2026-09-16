// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;

public class PrefixPolicy : INamingPolicy
{
    public JsonNamingPolicy JsonPropertyNamingPolicy { get; } = new PrefixJsonPolicy();
    public string GetReadModelName(Type readModelType) => readModelType.Name;
    public string GetPropertyName(string name) => JsonPropertyNamingPolicy.ConvertName(name);
}
