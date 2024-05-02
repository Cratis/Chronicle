// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
