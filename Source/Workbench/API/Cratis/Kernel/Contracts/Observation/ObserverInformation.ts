/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { ObserverType } from './ObserverType';
import { EventType } from '../Events/EventType';
import { ObserverRunningState } from './ObserverRunningState';

export class ObserverInformation {

    @field(String)
    observerId!: string;

    @field(String)
    eventSequenceId!: string;

    @field(String)
    name!: string;

    @field(ObserverType)
    type!: ObserverType;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(Number)
    nextEventSequenceNumber!: number;

    @field(ObserverRunningState)
    runningState!: ObserverRunningState;
}
