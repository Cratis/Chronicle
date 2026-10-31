// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ContextStorageIsolation;

/// <summary>
/// Records a fact counted by the storage-isolation specification.
/// </summary>
[EventType]
public record ContextEventRecorded;
