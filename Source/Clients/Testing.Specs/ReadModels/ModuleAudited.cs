// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test audit/marker event that coexists on a module's event stream but is not subscribed to by
/// <see cref="SimpleModule"/> — used to verify the harness ignores unsubscribed events the way the
/// production projection engine does.
/// </summary>
[EventType]
public record ModuleAudited;
