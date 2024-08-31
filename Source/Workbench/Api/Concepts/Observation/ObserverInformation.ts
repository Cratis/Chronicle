/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { ObserverType } from './ObserverType';
import { EventType } from '../Events/EventType';
import { ObserverRunningState } from './ObserverRunningState';
import { FailedPartition } from './FailedPartition';

export class ObserverInformation {

    @field(String)
    observerId!: string;

    @field(String)
    eventSequenceId!: string;

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

    @field(Number)
    handled!: number;

    @field(FailedPartition, true)
    failedPartitions!: FailedPartition[];
}
