/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { EventType } from './EventType';
import { ObserverType } from './ObserverType';
import { ObserverRunningState } from './ObserverRunningState';
import { FailedObserverPartition } from './FailedObserverPartition';
import { RecoveringFailedObserverPartition } from './RecoveringFailedObserverPartition';

export class ObserverState {

    @field(String)
    id!: string;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(String)
    eventSequenceId!: string;

    @field(String)
    observerId!: string;

    @field(String)
    name!: string;

    @field(Number)
    type!: ObserverType;

    @field(Number)
    nextEventSequenceNumber!: number;

    @field(Number)
    lastHandled!: number;

    @field(Number)
    runningState!: ObserverRunningState;

    @field(String)
    currentNamespace!: string;

    @field(FailedObserverPartition, true)
    failedPartitions!: FailedObserverPartition[];

    @field(RecoveringFailedObserverPartition, true)
    recoveringPartitions!: RecoveringFailedObserverPartition[];

    @field(Boolean)
    hasFailedPartitions!: boolean;

    @field(Boolean)
    isRecoveringAnyPartition!: boolean;
}
