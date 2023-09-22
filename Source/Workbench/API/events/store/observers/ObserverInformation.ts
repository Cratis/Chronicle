/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { ObserverType } from './ObserverType';
import { EventType } from './EventType';
import { ObserverRunningState } from './ObserverRunningState';
import { FailedPartition } from './FailedPartition';

export class ObserverInformation {

    @field(String)
    observerId!: string;

    @field(String)
    eventSequenceId!: string;

    @field(String)
    name!: string;

    @field(Number)
    type!: ObserverType;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(Number)
    nextEventSequenceNumber!: number;

    @field(Number)
    lastHandledEventSequenceNumber!: number;

    @field(Number)
    runningState!: ObserverRunningState;

    @field(FailedPartition, true)
    failedPartitions!: FailedPartition[];
}
