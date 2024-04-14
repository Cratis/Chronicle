/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';


export class AppendEvent {

    @field(String)
    eventSourceId!: string;

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;

    @field(Date)
    validFrom?: Date;
}
