// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.Setup.for_background_startup;

[EventType]
public record StartupDeliveryRecorded(string ObserverId, string InputToken, ulong InputSequenceNumber);
