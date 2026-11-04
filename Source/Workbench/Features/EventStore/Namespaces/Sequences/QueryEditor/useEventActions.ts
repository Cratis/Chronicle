// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useMemo } from 'react';
import { DialogButtons, DialogResult, useConfirmationDialog, useDialog } from '@cratis/arc.react/dialogs';
import strings from 'Strings';
import { AppendedEvent } from 'Features/Sequences';
import { GetReplayableObserversForEventTypes } from 'Features/Observation';
import { ObserveEventTypes } from 'Features/EventTypes';
import { ObserverType } from 'Features/Contracts/Observation';
import { AppendEventDialog } from '../Add/AppendEventDialog';
import { RedactEventDialog, RedactEventDialogProps } from '../RedactEventDialog';
import { ReviseDialog, ReviseDialogProps } from '../ReviseDialog';
import { CompensateDialog, CompensateDialogProps } from '../CompensateDialog';
import { getCompensationEventTypes } from '../getCompensationEventTypes';

/**
 * How long to wait after an event is written before re-reading, so the append has settled.
 */
const refreshDelayInMilliseconds = 200;

const observerTypeName = (type: ObserverType): string => {
    switch (type) {
        case ObserverType.reactor: return 'Reactor';
        case ObserverType.projection: return 'Projection';
        case ObserverType.reducer: return 'Reducer';
        case ObserverType.external: return 'External';
        default: return 'Unknown';
    }
};

/**
 * The actions that act on the events a query returned, rather than on the query itself.
 * @param eventStore The event store the events belong to.
 * @param namespace The namespace the events belong to.
 * @param eventSequenceId The event sequence the events belong to.
 * @param selectedEvent The event the user has selected, if any.
 * @param onChanged Called after an action changed the sequence, so results can be re-read.
 * @returns The dialog wrappers to render and the handlers to bind to toolbar actions.
 */
export const useEventActions = (
    eventStore: string,
    namespace: string,
    eventSequenceId: string,
    selectedEvent: AppendedEvent | null,
    onChanged: () => void
) => {
    const [AppendEventWrapper, showAppendEvent] = useDialog(AppendEventDialog);
    const [RedactEventWrapper, showRedactEvent] = useDialog<RedactEventDialogProps>(RedactEventDialog);
    const [ReviseWrapper, showRevise] = useDialog<ReviseDialogProps>(ReviseDialog);
    const [CompensateWrapper, showCompensate] = useDialog<CompensateDialogProps>(CompensateDialog);
    const [showConfirmation] = useConfirmationDialog();
    const [eventTypes] = ObserveEventTypes.use({ eventStore });

    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const refreshAfter = useCallback((result: DialogResult) => {
        if (result === DialogResult.Ok) {
            setTimeout(onChanged, refreshDelayInMilliseconds);
        }
    }, [onChanged]);

    const append = useCallback(async () => {
        const [result] = await showAppendEvent();
        refreshAfter(result);
    }, [showAppendEvent, refreshAfter]);

    const redact = useCallback(async () => {
        if (!selectedEvent) return;

        const confirmed = await showConfirmation(
            sequenceStrings.dialogs.redact.confirmTitle,
            sequenceStrings.dialogs.redact.confirmMessage,
            DialogButtons.YesNo);
        if (confirmed !== DialogResult.Yes) return;

        const [result] = await showRedactEvent({
            eventStore,
            namespace,
            eventSequenceId,
            sequenceNumber: selectedEvent.context.sequenceNumber
        });
        refreshAfter(result);
    }, [selectedEvent, showConfirmation, showRedactEvent, eventStore, namespace, eventSequenceId, sequenceStrings, refreshAfter]);

    const revise = useCallback(async () => {
        if (!selectedEvent) return;

        const observers = await new GetReplayableObserversForEventTypes().perform({
            eventStore,
            namespace,
            eventTypes: [selectedEvent.context.eventType.id]
        });

        const reviseStrings = sequenceStrings.dialogs.revise;
        const message = observers.data.length > 0
            ? `${reviseStrings.confirmMessage}\n\n${observers.data.map(observer => `• ${observer.id} (${observerTypeName(observer.type)})`).join('\n')}`
            : reviseStrings.confirmNoObservers;

        const confirmed = await showConfirmation(reviseStrings.confirmTitle, message, DialogButtons.YesNo);
        if (confirmed !== DialogResult.Yes) return;

        const [result] = await showRevise({ event: selectedEvent, eventStore, namespace });
        refreshAfter(result);
    }, [selectedEvent, showConfirmation, showRevise, eventStore, namespace, sequenceStrings, refreshAfter]);

    const compensationTypes = useMemo(
        () => selectedEvent ? getCompensationEventTypes(eventTypes.data, selectedEvent.context.eventType.id) : [],
        [eventTypes.data, selectedEvent]);

    const canCompensate = compensationTypes.length > 0;

    const compensate = useCallback(async () => {
        if (!selectedEvent || !canCompensate) return;

        const [result] = await showCompensate({ event: selectedEvent, eventStore, namespace, eventSequenceId, compensationTypes });
        refreshAfter(result);
    }, [selectedEvent, canCompensate, showCompensate, eventStore, namespace, eventSequenceId, compensationTypes, refreshAfter]);

    return { AppendEventWrapper, RedactEventWrapper, ReviseWrapper, CompensateWrapper, append, redact, revise, compensate, canCompensate };
};
