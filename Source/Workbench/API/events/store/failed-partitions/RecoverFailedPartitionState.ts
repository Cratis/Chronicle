/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { EventType } from './EventType';

export class RecoverFailedPartitionState {

    @field(String)
    id!: string;

    @field(String)
    partition!: string;

    @field(String)
    eventSequenceId!: string;

    @field(String)
    observerId!: string;

    @field(String)
    observerName!: string;

    @field(String)
    observerKey!: string;

    @field(String)
    subscriberKey!: string;

    @field(Number)
    initialError!: number;

    @field(Number)
    currentError!: number;

    @field(Number)
    nextSequenceNumberToProcess!: number;

    @field(Number)
    numberOfAttemptsOnSinceInitialized!: number;

    @field(Number)
    numberOfAttemptsOnCurrentError!: number;

    @field(Date)
    initialPartitionFailedOn!: Date;

    @field(Date)
    lastAttemptOnCurrentError?: Date;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(String)
    stackTrace!: string;

    @field(String, true)
    messages!: string[];
}
