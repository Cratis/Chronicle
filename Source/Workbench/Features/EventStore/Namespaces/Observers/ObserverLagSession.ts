// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ObserverLag } from 'Features/Observation';
import { ObserverLagController, type ObserverLagReader, type ObserverLagTarget, observerLagKey } from './ObserverLagController';

const refreshIntervalMs = 30_000;
const visibilityDebounceMs = 250;
const clockIntervalMs = 5_000;

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
 * The lifetime of reading the lag of one view's observers. It can be attached, detached and attached again, which is
 * what React does to a mounted component in strict mode, and each attach starts a fresh controller, so nothing read
 * before a detach is shown after it. What is on screen and what is selected outlive an attach, because the cells report
 * themselves before the view that owns the session attaches it.
 */
export class ObserverLagSession {
    private readonly options: ObserverLagSessionOptions;
    private readonly visible = new Map<string, ObserverLagTarget>();
    private controller: ObserverLagController | undefined;
    private selected: ObserverLagTarget | undefined;
    private detachFromPage: (() => void) | undefined;
    private intervals: ReturnType<typeof setInterval>[] = [];
    private expiryWake: ReturnType<typeof setTimeout> | undefined;
    private debounce: ReturnType<typeof setTimeout> | undefined;

    /** The moment the view judges measurements at; it moves with the clock and when a measurement arrives. */
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
        controller.select(this.selected);
        controller.setPaused(this.options.visibility.isHidden());
        this.detachFromPage = this.options.visibility.subscribe(() => this.pageVisibilityChanged());
        this.intervals = [
            setInterval(() => controller.refresh(), refreshIntervalMs),
            setInterval(() => { if (!this.options.visibility.isHidden()) this.tick(); }, clockIntervalMs)
        ];
        controller.refresh();
    }

    /** Stops reading, aborts what is outstanding and forgets what was read. */
    detach() {
        this.detachFromPage?.();
        this.detachFromPage = undefined;
        this.intervals.forEach(clearInterval);
        this.intervals = [];
        clearTimeout(this.expiryWake);
        clearTimeout(this.debounce);
        this.controller?.dispose();
        this.controller = undefined;
    }

    /** Selects the target the detail shows, or none. */
    select(target: ObserverLagTarget | undefined) {
        const changed = (target ? observerLagKey(target) : undefined) !== (this.selected ? observerLagKey(this.selected) : undefined);
        this.selected = target;
        if (changed) this.controller?.select(target);
    }

    /** Records that a target scrolled into or out of view, and reads what became visible once the scrolling settles. */
    setVisible(target: ObserverLagTarget, isVisible: boolean) {
        if (isVisible) this.visible.set(observerLagKey(target), target);
        else this.visible.delete(observerLagKey(target));

        this.controller?.setVisible(target, isVisible);
        clearTimeout(this.debounce);
        this.debounce = setTimeout(() => this.controller?.refresh(), visibilityDebounceMs);
    }

    private pageVisibilityChanged() {
        const hidden = this.options.visibility.isHidden();
        this.controller?.setPaused(hidden);
        // Time passed while hidden, so what was current may not be any more: judge it again before anything is shown.
        if (!hidden) this.tick();
    }

    private tick() {
        this.now = Date.now();
        this.options.onChange();
        this.scheduleExpiryWake();
    }

    private measurementArrived() {
        this.now = Date.now();
        this.options.onChange();
        this.scheduleExpiryWake();
    }

    /** Wakes the view just after the nearest measurement stops describing its observer, wherever the clock is. */
    private scheduleExpiryWake() {
        clearTimeout(this.expiryWake);
        const expiry = this.controller?.nextExpiryAfter(Date.now());
        if (expiry === undefined) return;

        this.expiryWake = setTimeout(() => this.tick(), Math.max(0, expiry - Date.now()) + 1);
    }
}
