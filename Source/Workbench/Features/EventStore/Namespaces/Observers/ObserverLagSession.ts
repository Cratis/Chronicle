// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ObserverLag } from 'Features/Observation';
import { ObserverLagController, type ObserverLagReader, type ObserverLagTarget, observerLagKey } from './ObserverLagController';

const refreshIntervalMs = 30_000;
const minBatchIntervalMs = 250;
const clockIntervalMs = 5_000;

/** The longest delay a timer honours; a longer one fires at once. */
export const maxTimerDelayMs = 2 ** 31 - 1;

/** How long to wait from now until the earliest due moment, never negative and never more than a timer can wait. */
export const boundedDelay = (due: number[], now: number) =>
    Math.min(maxTimerDelayMs, Math.max(0, Math.min(...due) - now));

/** Where the session learns whether the page is shown. */
export interface PageVisibility {
    isHidden(): boolean;

    /** Calls back when the page is hidden or shown; returns what stops it. */
    subscribe(onChange: () => void): () => void;
}

export interface ObserverLagSessionOptions {
    reader: ObserverLagReader;
    visibility: PageVisibility;

    /** Called when what is shown may have changed: a measurement arrived, or the clock moved. */
    onChange: () => void;
}

/**
 * The lifetime of reading the lag of one view's observers, driven by one scheduler. Everything that could call for a read
 * (attaching, a selection, the viewport, the page being shown, the poll, an expiry) only marks the session dirty and asks
 * for a pump. The pump does nothing while the session is detached or the page hidden, never runs batches closer together
 * than the minimum interval, and after each run sets exactly one timer for whatever comes next: the poll, the nearest
 * measurement expiry or the clock. It can be attached, detached and attached again, as React does in strict mode, and each
 * attach starts a fresh controller, so nothing read before a detach is shown after it. What is on screen and what is
 * selected outlive an attach, because the cells report themselves before the view that owns the session attaches it.
 */
export class ObserverLagSession {
    private readonly options: ObserverLagSessionOptions;
    private readonly visible = new Map<string, ObserverLagTarget>();
    private controller: ObserverLagController | undefined;
    private selected: ObserverLagTarget | undefined;
    private detachFromPage: (() => void) | undefined;
    private timer: ReturnType<typeof setTimeout> | undefined;
    private armedFor = Number.POSITIVE_INFINITY;
    private dirty = false;
    private selectionDirty = false;
    private lastBatchAt = Number.NEGATIVE_INFINITY;
    private nextPollAt = 0;
    private nextClockAt = 0;

    /** The moment the view judges measurements at; it moves with every pump and when a measurement arrives. */
    now = Date.now();

    constructor(options: ObserverLagSessionOptions) {
        this.options = options;
    }

    get isAttached() {
        return this.controller !== undefined;
    }

    /** The last measurement read for the target, or undefined when none has been, or the read failed. */
    lagFor(target: ObserverLagTarget): ObserverLag | undefined {
        return this.controller?.get(target);
    }

    /** Whether a read for the target has completed, failed reads included. */
    hasResult(target: ObserverLagTarget): boolean {
        return this.controller?.hasResult(target) ?? false;
    }

    /** Starts reading. Does nothing when attached already. */
    attach() {
        if (this.controller) return;

        const controller = new ObserverLagController({ reader: this.options.reader, onChange: () => this.measurementArrived() });
        this.controller = controller;
        this.visible.forEach(target => controller.setVisible(target, true));
        controller.setPaused(this.isHidden);
        this.selectionDirty = true;
        this.detachFromPage = this.options.visibility.subscribe(() => this.pageVisibilityChanged());
        this.request();
    }

    /** Stops reading, aborts what is outstanding and forgets what was read. */
    detach() {
        this.detachFromPage?.();
        this.detachFromPage = undefined;
        this.disarm();
        this.controller?.dispose();
        this.controller = undefined;
        this.dirty = false;
        this.lastBatchAt = Number.NEGATIVE_INFINITY;
    }

    /** Selects the target the detail shows, or none. */
    select(target: ObserverLagTarget | undefined) {
        if (this.keyOf(target) === this.keyOf(this.selected)) return;
        this.selected = target;
        this.selectionDirty = true;
        this.request();
    }

    /** Records that a target scrolled into or out of view. */
    setVisible(target: ObserverLagTarget, isVisible: boolean) {
        if (isVisible) this.visible.set(observerLagKey(target), target);
        else this.visible.delete(observerLagKey(target));

        this.controller?.setVisible(target, isVisible);
        this.request();
    }

    private get isHidden() {
        return this.options.visibility.isHidden();
    }

    private keyOf(target: ObserverLagTarget | undefined) {
        return target ? observerLagKey(target) : undefined;
    }

    private request() {
        this.dirty = true;
        this.schedule();
    }

    private pageVisibilityChanged() {
        const hidden = this.isHidden;
        this.controller?.setPaused(hidden);
        if (hidden) return;

        // Time passed while hidden: judge what was read again, and read again.
        this.now = Date.now();
        this.options.onChange();
        this.request();
    }

    private measurementArrived() {
        this.now = Date.now();
        this.options.onChange();
        this.schedule();
    }

    private disarm() {
        clearTimeout(this.timer);
        this.timer = undefined;
        this.armedFor = Number.POSITIVE_INFINITY;
    }

    /**
     * Arms the one timer for the earliest of what is due, never closer than the minimum interval to the last batch.
     * A trigger can only move an armed wake earlier, never later, so steady triggers such as scrolling cannot starve the
     * poll or an expiry. Expiries are found from the last judged time, so one the view has not processed yet still wakes,
     * and the delay is measured from a fresh reading of the clock.
     */
    private schedule() {
        if (!this.controller || this.isHidden) {
            this.disarm();
            return;
        }

        const due = [this.nextPollAt, this.nextClockAt];
        if (this.dirty) due.push(this.lastBatchAt + minBatchIntervalMs);

        const expiry = this.controller.nextExpiryAfter(this.now);
        if (expiry !== undefined) due.push(expiry + 1);

        const wake = Math.min(...due);
        if (this.timer !== undefined && this.armedFor <= wake) return;

        clearTimeout(this.timer);
        this.armedFor = wake;
        this.timer = setTimeout(() => this.pump(), boundedDelay([wake], Date.now()));
    }

    private pump() {
        this.timer = undefined;
        this.armedFor = Number.POSITIVE_INFINITY;
        const controller = this.controller;
        if (!controller || this.isHidden) return;

        this.now = Date.now();
        const batchDue = this.dirty && this.now >= this.lastBatchAt + minBatchIntervalMs;
        if (batchDue || this.now >= this.nextPollAt) {
            // The selection is applied first so it has the first claim on free slots.
            if (this.selectionDirty) controller.select(this.selected);
            controller.refresh();
            this.selectionDirty = false;
            this.dirty = false;
            this.lastBatchAt = this.now;
            this.nextPollAt = this.now + refreshIntervalMs;
        }
        if (this.now >= this.nextClockAt) this.nextClockAt = this.now + clockIntervalMs;

        this.options.onChange();
        this.schedule();
    }
}
