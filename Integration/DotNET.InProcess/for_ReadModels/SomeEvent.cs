// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels;

[EventType("1c0f19dd-e9fc-4a71-bfa8-bf5eefb12b52")]
public record SomeEvent(int Number);
