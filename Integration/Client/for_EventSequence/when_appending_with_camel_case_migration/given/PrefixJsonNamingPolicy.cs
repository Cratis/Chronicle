// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;

public class PrefixJsonNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => $"mapped_{name}";
}
