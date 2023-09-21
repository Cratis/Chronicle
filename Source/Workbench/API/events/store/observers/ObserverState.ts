/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';
import { ObserverType } from './ObserverType';
import { ObserverRunningState } from './ObserverRunningState';
import { TailEventSequenceNumbers } from './TailEventSequenceNumbers';

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

    @field(TailEventSequenceNumbers)
    tailEventSequenceNumbers!: TailEventSequenceNumbers;
}
