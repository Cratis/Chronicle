/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';
import { FailedPartitionAttempt } from './FailedPartitionAttempt';

export class FailedPartition {

    @field(String)
    id!: string;

    @field(String)
    partition!: string;

    @field(String)
    eventSequenceId!: string;

    @field(String)
    observerId!: string;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(FailedPartitionAttempt, true)
    attempts!: FailedPartitionAttempt[];

    @field(Boolean)
    isResolved!: boolean;
}
