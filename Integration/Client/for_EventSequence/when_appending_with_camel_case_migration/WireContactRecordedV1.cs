// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

[EventTypeGenerationFor<WireContactRecorded>(1)]
public record WireContactRecordedV1([property: JsonPropertyName("ContactDetails")] WireContactDetailsV1 Contact);
