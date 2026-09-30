// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverRunningState } from 'Features/Contracts/Observation';
import { lagFor, ObserverLike } from './health';

/**
 * What an item needing attention is about.
 */
export type AttentionKind = 'failedPartition' | 'observer' | 'lagging' | 'recommendation';

/**
 * Something on a namespace that needs looking at.
 */
export interface AttentionItem {
    key: string;
    kind: AttentionKind;
    tone: 'danger' | 'warning' | 'info';
    subject: string;
    detail: string;
    count?: number;
}

/**
 * The shape of a failed partition the attention list needs.
 */
export interface AttentionFailedPartition {
    id: { toString(): string };
    observerId: string;
    partition: string;
    isResolved: boolean;
    attempts: { messages: string[] }[];
}

/**
 * The shape of a recommendation the attention list needs.
 */
export interface AttentionRecommendation {
    id: { toString(): string };
    name: string;
    description: string;
}

/**
 * Collect what needs attention on a namespace, most severe first.
 * @param observers The observers of the namespace.
 * @param failedPartitions The failed partitions of the namespace.
 * @param recommendations The pending recommendations of the namespace.
 * @param limit How many items to return at most.
 * @returns The items.
 */
export const collectAttention = (
    observers: ObserverLike[],
    failedPartitions: AttentionFailedPartition[],
    recommendations: AttentionRecommendation[],
    limit: number): AttentionItem[] => {
    const failed: AttentionItem[] = failedPartitions
        .filter(_ => !_.isResolved)
        .map(_ => {
            const lastAttempt = _.attempts.length > 0 ? _.attempts[_.attempts.length - 1] : undefined;
            const messages = (lastAttempt?.messages ?? []).filter(message => message.trim().length > 0);

            // The messages run from the outermost exception inwards, and the outermost is often a wrapper such as a
            // reflection invocation exception that says nothing about what went wrong - the innermost does.
            const reason = messages.length > 0 ? messages[messages.length - 1] : undefined;
            return {
                key: `failed-${_.id.toString()}`,
                kind: 'failedPartition',
                tone: 'danger',
                subject: _.observerId,
                detail: reason ? `${_.partition} · ${reason}` : _.partition,
                count: _.attempts.length
            };
        });

    const stopped: AttentionItem[] = observers
        .filter(_ => _.runningState === ObserverRunningState.disconnected || _.runningState === ObserverRunningState.quarantined)
        .map(_ => ({
            key: `observer-${_.id}`,
            kind: 'observer',
            tone: _.runningState === ObserverRunningState.quarantined ? 'danger' : 'warning',
            subject: _.id,
            detail: _.runningState === ObserverRunningState.quarantined ? 'quarantined' : 'disconnected'
        }));

    const suggested: AttentionItem[] = recommendations.map(_ => ({
        key: `recommendation-${_.id.toString()}`,
        kind: 'recommendation',
        tone: 'warning',
        subject: _.name,
        detail: _.description
    }));

    const lagging: AttentionItem[] = observers
        .map(_ => ({ observer: _, lag: lagFor(_) }))
        .filter(_ => _.lag > 0)
        .sort((left, right) => right.lag - left.lag)
        .map(_ => ({
            key: `lagging-${_.observer.id}`,
            kind: 'lagging',
            tone: 'info',
            subject: _.observer.id,
            detail: 'behind',
            count: _.lag
        }));

    return [...failed, ...stopped, ...suggested, ...lagging].slice(0, limit);
};
