// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

[EventTypeGenerationFor<ContactRecorded>(1)]
public record ContactRecordedV1(ContactDetailsV1 Contact);
