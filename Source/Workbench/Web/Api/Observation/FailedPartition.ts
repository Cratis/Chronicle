/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { FailedPartitionAttempt } from './FailedPartitionAttempt';

export class FailedPartition {

    @field(Guid)
    id!: Guid;

    @field(String)
    observerId!: string;

    @field(String)
    partition!: string;

    @field(FailedPartitionAttempt, true)
    attempts!: FailedPartitionAttempt[];
}
