/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { ObserverType } from './ObserverType';
import { EventType } from './EventType';
import { ObserverRunningState } from './ObserverRunningState';

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
    runningState!: ObserverRunningState;
}
