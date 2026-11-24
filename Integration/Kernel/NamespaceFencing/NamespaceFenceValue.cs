// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[EventType("3d60f954-8caf-4a99-9bf2-51319c1c05ae")]
public record NamespaceFenceValue(string Value);
