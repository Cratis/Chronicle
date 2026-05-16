// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_for_event_source_id_and_event_types;

[EventType]
public record SomeEventWithPii(string Name, [property: PII] string SocialSecurityNumber);
