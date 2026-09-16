// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

public record WireContactDetails([property: JsonPropertyName("WireEmail")] string Email, string Phone, string Label);
