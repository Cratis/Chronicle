// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const webhookGoneCondition = 'webhook-gone';

/** Identifies a terminal webhook incident within the already-scoped observer query. */
export const isWebhookGone = (incidents: readonly { condition: string; target: { eventSequenceId: string } }[], eventSequenceId: string): boolean =>
    incidents.some(incident => incident.condition === webhookGoneCondition && incident.target.eventSequenceId === eventSequenceId);
