/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { FailedPartitionAttempt } from './FailedPartitionAttempt';
import { Key } from '../Keys/Key';

export class FailedPartition {

    @field(Guid)
    id!: Guid;

    @field(Key)
    partition!: Key;

    @field(String)
    observerId!: string;

    @field(FailedPartitionAttempt, true)
    attempts!: FailedPartitionAttempt[];

    @field(Boolean)
    isResolved!: boolean;

    @field(FailedPartitionAttempt)
    lastAttempt!: FailedPartitionAttempt;
}
