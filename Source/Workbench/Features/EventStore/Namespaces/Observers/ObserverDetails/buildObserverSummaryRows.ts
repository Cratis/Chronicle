// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Api/Observation';
import { ObserverSummaryRow } from './ObserverSummaryRow';
import { getObserverRunningStateAsText } from '../getObserverRunningStateAsText';
import { renderObserverOwner } from './renderObserverOwner';
import { renderObserverType } from './renderObserverType';
import strings from 'Strings';

const renderBoolean = (value: boolean): string => (value ? 'Yes' : 'No');

/**
 * Build the label/value rows shown in the observer summary table.
 *
 * @param observer - The observer to summarize.
 * @returns An array of {@link ObserverSummaryRow} ready for display.
 */
export const buildObserverSummaryRows = (observer: ObserverInformation): ObserverSummaryRow[] => {
    const summaryStrings = strings.eventStore.namespaces.observers.details.summary;

    return [
        { label: summaryStrings.id, value: observer.id },
        { label: summaryStrings.type, value: renderObserverType(observer) },
        { label: summaryStrings.owner, value: renderObserverOwner(observer) },
        { label: summaryStrings.state, value: getObserverRunningStateAsText(observer.runningState) },
        { label: summaryStrings.sequence, value: observer.eventSequenceId },
        { label: summaryStrings.nextEventSequenceNumber, value: observer.nextEventSequenceNumber },
        { label: summaryStrings.lastHandledEventSequenceNumber, value: observer.lastHandledEventSequenceNumber },
        { label: summaryStrings.tailEventSequenceNumber, value: observer.tailEventSequenceNumber },
        { label: summaryStrings.isSubscribed, value: renderBoolean(observer.isSubscribed) },
        { label: summaryStrings.isReplayable, value: renderBoolean(observer.isReplayable) }
    ];
};
