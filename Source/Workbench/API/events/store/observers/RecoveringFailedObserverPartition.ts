/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';


export class RecoveringFailedObserverPartition {
    @field(String)
    eventSourceId!: string;
    @field(Number)
    sequenceNumber!: number;
    @field(Date)
    startedRecoveryAt!: Date;
}
