// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Kernel.Compliance.GDPR.Events;

/// <summary>
/// Represents the event that gets applied when personal information for a person is deleted.
/// </summary>
[EventType("5e938436-a24b-4886-af5d-90f31d08da55")]
public record PersonalInformationForPersonDeleted();
