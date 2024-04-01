/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class RedactEvent {

    @field(Number)
    sequenceNumber!: number;

    @field(String)
    reason!: string;

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;
}
