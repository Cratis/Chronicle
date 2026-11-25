// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent, EventRevision } from 'Features/Sequences';
import { ComparisonBudget } from './ComparisonBudget';
import { RevisionChoice } from './RevisionChoice';
import { RevisionOffer } from './RevisionOffer';

const redactedEventType = 'EventRedacted';
const originalKey = 'original';
const originalGeneration = 1;

const none = (state: RevisionOffer['state']): RevisionOffer => ({ state, choices: [], ambiguous: 0 });

const identify = (revision: EventRevision, ordinal: number): RevisionChoice | undefined => {
    const occurred = new Date(revision.occurred as unknown as string | number | Date);
    const correlationId = revision.correlationId;
    const identified = !Number.isNaN(occurred.getTime())
        && typeof correlationId === 'string' && correlationId !== '' && correlationId.length <= ComparisonBudget.maxIdentity
        && Number.isInteger(revision.generation);
    if (!identified) return undefined;
    const iso = occurred.toISOString();
    return {
        key: JSON.stringify(['revision', iso, correlationId, revision.generation]),
        ordinal,
        generation: revision.generation,
        occurred: iso,
        correlationId,
        content: revision.content
    };
};

const original = (event: AppendedEvent): RevisionChoice => {
    const occurred = new Date(event.context.occurred as unknown as string | number | Date);
    return {
        key: originalKey,
        ordinal: 0,
        generation: originalGeneration,
        occurred: Number.isNaN(occurred.getTime()) ? '' : occurred.toISOString(),
        correlationId: String(event.context.correlationId ?? '').slice(0, ComparisonBudget.maxIdentity),
        content: event.originalContent
    };
};

/**
 * Lists the revisions an event offers for comparison from the released response alone. Redaction
 * takes precedence over everything else, including history a stale or hostile response still
 * carries. Revisions without a usable identity, or whose identity is shared, are left out rather
 * than guessed at.
 * @param event The event as received, if any.
 * @returns The state and the choices.
 */
export const resolveRevisionChoices = (event: AppendedEvent | undefined): RevisionOffer => {
    if (!event) return none('noHistory');
    if (event.context.eventType.id === redactedEventType) return none('redacted');
    if ((event.erasedProperties?.length ?? 0) > 0 || (event.erasedPropertiesForJson?.length ?? 0) > 0) return none('erased');
    const revisions = event.revisions ?? [];
    if (revisions.length === 0) return none('noHistory');
    if (revisions.length > ComparisonBudget.maxRevisions) return none('limited');

    const identified = revisions.map((revision, index) => identify(revision, index + 1));
    const counts = new Map<string, number>();
    identified.forEach(choice => choice && counts.set(choice.key, (counts.get(choice.key) ?? 0) + 1));
    const usable = identified.filter((choice): choice is RevisionChoice => choice !== undefined && counts.get(choice.key) === 1);
    return { state: 'ready', choices: [original(event), ...usable], ambiguous: revisions.length - usable.length };
};
