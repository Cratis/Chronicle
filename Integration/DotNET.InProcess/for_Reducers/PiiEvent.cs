// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers;

[EventType]
public record PiiEvent(int Number, [property: PII] string SocialSecurityNumber);
