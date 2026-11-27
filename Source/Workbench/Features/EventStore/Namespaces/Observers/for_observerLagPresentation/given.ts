// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverLag, ObserverLagCause, ObserverLagStatus } from 'Features/Observation';

export const sampledAt = new Date('2025-01-01T12:00:00Z');
export const expiresAt = new Date('2025-01-01T12:02:00Z');
export const during = sampledAt.getTime() + 30_000;

export const lag = (values: Partial<ObserverLag> = {}) => Object.assign(new ObserverLag(), {
    status: ObserverLagStatus.lagging,
    cause: ObserverLagCause.unknown,
    sampledAt,
    expiresAt,
    ...values
});
