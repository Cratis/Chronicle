// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { ObserverLag, ObserverLagStatus } from 'Features/Observation';
import { ObserverLagSession, type PageVisibility } from '../../ObserverLagSession';
import { type ObserverLagTarget } from '../../ObserverLagController';

export const target = (observerId: string): ObserverLagTarget =>
    ({ eventStore: 'store', namespace: 'ns', eventSequenceId: 'event-log', observerId });

/** A session over a reader that answers at once with a sample valid for the given time, on fake timers. */
export class a_session {
    hidden = false;
    changes = 0;
    reads: string[] = [];
    validityMs = 60_000;
    private listeners: (() => void)[] = [];
    session: ObserverLagSession;

    constructor() {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2025-01-01T12:00:00Z'));
        const visibility: PageVisibility = {
            isHidden: () => this.hidden,
            subscribe: onChange => {
                this.listeners.push(onChange);
                return () => { this.listeners = this.listeners.filter(listener => listener !== onChange); };
            }
        };
        this.session = new ObserverLagSession({
            visibility,
            onChange: () => this.changes++,
            reader: async readTarget => {
                this.reads.push(readTarget.observerId);
                const now = Date.now();
                return Object.assign(new ObserverLag(), {
                    status: ObserverLagStatus.lagging,
                    distance: 1,
                    sampledAt: new Date(now),
                    expiresAt: new Date(now + this.validityMs)
                });
            }
        });
    }

    setHidden(hidden: boolean) {
        this.hidden = hidden;
        this.listeners.forEach(listener => listener());
    }

    async settle() {
        await vi.advanceTimersByTimeAsync(0);
    }

    dispose() {
        this.session.detach();
        vi.useRealTimers();
    }
}
