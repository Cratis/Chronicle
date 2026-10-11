// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;

[EventTypeGenerationFor<PinnedPersonRegistered>(1)]
public record PinnedPersonRegisteredV1([property: PII] string Name);
