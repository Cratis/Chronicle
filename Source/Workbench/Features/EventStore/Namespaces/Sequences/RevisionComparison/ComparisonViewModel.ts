// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { AppendedEvent } from 'Features/Sequences';
import { ComparisonScope } from './ComparisonScope';
import { ContentComparison } from './ContentComparison';
import { RevisionChoice } from './RevisionChoice';
import { RevisionOffer } from './RevisionOffer';
import { compareContent } from './compareContent';
import { resolveRevisionChoices } from './revisionChoices';

export interface ComparisonProps {
    scope: ComparisonScope;
    event: AppendedEvent;
}

/**
 * Read-only comparison of the retained revisions of one event. It keeps only what the user chose
 * (two stable choice keys) and derives everything else from the latest response, so nothing about
 * a payload outlives the response that carried it. It never queries, refreshes or writes.
 */
@injectable()
export class ComparisonViewModel {
    isOpen = false;
    leftKey?: string;
    rightKey?: string;
    private _event?: AppendedEvent;
    private _identity = '';

    handleProps(props: ComparisonProps) {
        const identity = JSON.stringify([props.scope.eventStore, props.scope.namespace, props.scope.eventSequenceId, props.event.id ?? props.event.context.sequenceNumber]);
        if (identity !== this._identity) this.reset();
        this._identity = identity;
        this._event = props.event;
        if (this.revisions.state !== 'ready') this.clearSelection();
        else if (this.isOpen && this.leftKey === undefined && this.rightKey === undefined) this.selectDefaults();
    }

    get revisions(): RevisionOffer {
        return resolveRevisionChoices(this._event);
    }

    get left(): RevisionChoice | undefined {
        return this.revisions.choices.find(choice => choice.key === this.leftKey);
    }

    get right(): RevisionChoice | undefined {
        return this.revisions.choices.find(choice => choice.key === this.rightKey);
    }

    /** The comparison, or undefined while closed or when the event offers none. A side that has gone is reported unavailable, never replaced. */
    get comparison(): ContentComparison | undefined {
        if (!this.isOpen || this.revisions.state !== 'ready') return undefined;
        const [left, right] = [this.left, this.right];
        if (!left) return { outcome: 'unavailable', rows: [], side: 'left' };
        if (!right) return { outcome: 'unavailable', rows: [], side: 'right' };
        return compareContent(left.content, right.content);
    }

    open() {
        this.isOpen = true;
        this.selectDefaults();
    }

    private selectDefaults() {
        const { choices } = this.revisions;
        if (choices.length === 0) return;
        // Resolve "latest" once to a concrete identity so later revisions do not move the selection.
        this.leftKey = (choices.find(choice => choice.content) ?? choices[0]).key;
        this.rightKey = choices[choices.length - 1].key;
    }

    close() {
        this.isOpen = false;
        this.clearSelection();
    }

    selectLeft(key: string) {
        this.leftKey = key;
    }

    selectRight(key: string) {
        this.rightKey = key;
    }

    private reset() {
        this.isOpen = false;
        this.clearSelection();
    }

    private clearSelection() {
        this.leftKey = undefined;
        this.rightKey = undefined;
    }
}
