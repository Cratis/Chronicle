// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Acknowledges application of one immutable observer report, never merely its acceptance.
/// </summary>
/// <param name="LifecycleId">The reported lifecycle.</param>
/// <param name="Revision">The reported revision.</param>
/// <param name="Outcome">The application outcome.</param>
/// <param name="QuarantineEpisodeId">A one-time legacy adoption proposal; persist it before ordinary reporting.</param>
public record ObserverAlertReceipt(Guid LifecycleId, long Revision, ObserverAlertReconciliation Outcome, Guid? QuarantineEpisodeId = null);
