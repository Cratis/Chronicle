// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ObserverLag } from 'Features/Observation';

/** Identifies one observer by everything that scopes it; an observer id alone is not unique. */
export interface ObserverLagTarget {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    observerId: string;
}

/** Reads the sampled measurement of one observer. Resolves undefined when it could not be read; never a made-up measurement. */
export type ObserverLagReader = (target: ObserverLagTarget, signal: AbortSignal) => Promise<ObserverLag | undefined>;

export interface ObserverLagControllerOptions {
    reader: ObserverLagReader;

    /** Called when a measurement arrived and what is shown may have changed. */
    onChange: () => void;

    /** The most visible observers read in one cycle; the selected observer comes on top of these. */
    maxPerCycle?: number;

    /** The most reads outstanding at once, aborted ones included until they settle. */
    maxOutstanding?: number;

    /** The most measurements remembered; observers that are not on screen are forgotten first. */
    maxEntries?: number;
}

/** The key that identifies a target in every map of the controller. */
export const observerLagKey = (target: ObserverLagTarget) =>
    JSON.stringify([target.eventStore, target.namespace, target.eventSequenceId, target.observerId]);

/**
 * Reads the sampled lag of the observers a view shows, a bounded number at a time. It is created per mounted view and
 * disposed with it; a read that finishes after that, or for an observer no longer shown, is discarded.
 */
export class ObserverLagController {
    private readonly reader: ObserverLagReader;
    private readonly onChange: () => void;
    private readonly maxPerCycle: number;
    private readonly maxOutstanding: number;
    private readonly maxEntries: number;

    private readonly cache = new Map<string, ObserverLag | undefined>();
    private readonly targets = new Map<string, ObserverLagTarget>();
    private readonly visible = new Set<string>();
    private readonly inFlight = new Map<string, AbortController>();
    private queue: string[] = [];
    private selectedKey: string | undefined;
    private outstanding = 0;
    private cursor = 0;
    private generation = 0;
    private paused = false;
    private disposed = false;

    constructor(options: ObserverLagControllerOptions) {
        this.reader = options.reader;
        this.onChange = options.onChange;
        this.maxPerCycle = options.maxPerCycle ?? 20;
        this.maxOutstanding = options.maxOutstanding ?? 4;
        this.maxEntries = options.maxEntries ?? 200;
    }

    /** True while reads are queued or outstanding, so a new cycle does not overlap. */
    get isBusy() {
        return this.queue.length > 0 || this.outstanding > 0;
    }

    /** The last measurement read for the target, or undefined when none has been, or the read failed. */
    get(target: ObserverLagTarget): ObserverLag | undefined {
        return this.cache.get(observerLagKey(target));
    }

    /** Whether a read for the target has completed, failed reads included. */
    hasResult(target: ObserverLagTarget): boolean {
        return this.cache.has(observerLagKey(target));
    }

    /** Records that a target scrolled into or out of view. Starts nothing; call {@link refresh}. */
    setVisible(target: ObserverLagTarget, isVisible: boolean) {
        const key = observerLagKey(target);
        if (isVisible) {
            this.visible.add(key);
            this.targets.set(key, target);
        } else {
            this.visible.delete(key);
            if (key !== this.selectedKey) this.targets.delete(key);
        }
    }

    /** Selects the target the detail shows, or none. It is read ahead of everything else. */
    select(target: ObserverLagTarget | undefined) {
        const previous = this.selectedKey;
        this.selectedKey = target ? observerLagKey(target) : undefined;
        if (this.isLeftBehind(previous)) {
            this.targets.delete(previous);
            this.inFlight.get(previous)?.abort();
        }
        if (!target || this.disposed) return;

        this.targets.set(this.selectedKey!, target);
        this.queue = [this.selectedKey!, ...this.queue.filter(key => key !== this.selectedKey)];
        this.pump();
    }

    /** Stops reading while the page is hidden, and starts again when it is not. */
    setPaused(paused: boolean) {
        if (this.paused === paused || this.disposed) return;
        this.paused = paused;
        if (paused) {
            this.queue = [];
            this.inFlight.forEach(controller => controller.abort());
        } else {
            this.refresh();
        }
    }

    /**
     * Starts a cycle: the selected target and up to the cycle limit of visible ones, rotating through the visible
     * ones fairly. While a cycle is under way it only adds visible targets that were never read.
     */
    refresh() {
        if (this.disposed || this.paused) return;

        const candidates = [...this.visible].filter(key => key !== this.selectedKey);
        if (this.isBusy) {
            this.queue.push(...candidates.filter(key => this.isUnread(key)));
        } else {
            if (this.selectedKey) this.queue.push(this.selectedKey);
            this.queue.push(...this.nextCycle(candidates));
        }
        this.pump();
    }

    /** Aborts what is outstanding and forgets everything; nothing that finishes afterwards is kept. */
    dispose() {
        this.disposed = true;
        this.generation++;
        this.queue = [];
        this.inFlight.forEach(controller => controller.abort());
        this.cache.clear();
        this.targets.clear();
        this.visible.clear();
    }

    private isUnread(key: string) {
        return !this.cache.has(key) && !this.inFlight.has(key) && !this.queue.includes(key);
    }

    /** Takes the next slice of the visible targets, continuing where the last cycle stopped. */
    private nextCycle(candidates: string[]) {
        const count = Math.min(this.maxPerCycle, candidates.length);
        const picks = Array.from({ length: count }, (_, offset) => candidates[(this.cursor + offset) % candidates.length]);
        this.cursor = count === 0 ? 0 : (this.cursor + count) % candidates.length;
        return picks;
    }

    private get hasCapacity() {
        return !this.paused && !this.disposed && this.outstanding < this.maxOutstanding;
    }

    private isLeftBehind(previous: string | undefined): previous is string {
        return previous !== undefined && previous !== this.selectedKey && !this.visible.has(previous);
    }

    private isWanted(key: string) {
        return key === this.selectedKey || this.visible.has(key);
    }

    private pump() {
        // Work for something no longer on screen is dropped before it costs a request.
        this.queue = this.queue.filter(key => this.isWanted(key));
        while (this.hasCapacity && this.queue.length > 0) {
            const key = this.queue.shift()!;
            if (this.canStart(key)) this.start(key);
        }
    }

    private canStart(key: string) {
        return !this.inFlight.has(key) && this.targets.has(key);
    }

    private start(key: string) {
        const controller = new AbortController();
        const generation = this.generation;
        this.inFlight.set(key, controller);
        this.outstanding++;

        // Outstanding is released only when the read settles, so an aborted read still counts against the cap.
        this.reader(this.targets.get(key)!, controller.signal)
            .catch(() => undefined)
            .then(lag => this.complete(key, generation, controller, lag));
    }

    private complete(key: string, generation: number, controller: AbortController, lag: ObserverLag | undefined) {
        this.outstanding--;
        if (this.inFlight.get(key) === controller) this.inFlight.delete(key);

        if (this.isCurrent(key, generation, controller)) {
            this.cache.delete(key);
            this.cache.set(key, lag);
            this.evict();
            this.onChange();
        }

        this.pump();
    }

    private isCurrent(key: string, generation: number, controller: AbortController) {
        return generation === this.generation && !controller.signal.aborted && this.isWanted(key);
    }

    private evict() {
        for (const key of this.cache.keys()) {
            if (this.cache.size <= this.maxEntries) return;
            if (!this.isWanted(key)) this.cache.delete(key);
        }
    }
}
