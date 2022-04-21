/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { EventType } from './EventType';
import { ObserverType } from './ObserverType';
import { ObserverRunningState } from './ObserverRunningState';
import { FailedObserverPartition } from './FailedObserverPartition';
import { RecoveringFailedObserverPartition } from './RecoveringFailedObserverPartition';
import { Boolean } from './Boolean';

export type ObserverState = {
    id: string;
    eventTypes: EventType[];
    eventSequenceId: string;
    observerId: string;
    name: string;
    type: ObserverType;
    offset: number;
    lastHandled: number;
    runningState: ObserverRunningState;
    currentNamespace: string;
    failedPartitions: FailedObserverPartition[];
    recoveringPartitions: RecoveringFailedObserverPartition[];
    hasFailedPartitions: Boolean;
    isRecoveringAnyPartition: Boolean;
};
