/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';


export class RedactEvents {

    @field(String)
    eventSourceId!: string;

    @field(String)
    reason!: string;

    @field(String, true)
    eventTypes!: string[];

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;
}
