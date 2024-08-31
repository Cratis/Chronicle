/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { ObserverType } from './ObserverType';
import { EventType } from '../Events/EventType';
import { ObserverRunningState } from './ObserverRunningState';

export class ObserverInformation {

    @field(Guid)
    observerId!: Guid;

    @field(Guid)
    eventSequenceId!: Guid;

    @field(Number)
    type!: ObserverType;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(Number)
    nextEventSequenceNumber!: number;

    @field(Number)
    runningState!: ObserverRunningState;
}
