/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class AppendManyEvents {

    @field(String)
    eventSourceId!: string;

    @field(EventToAppend, true)
    events!: EventToAppend[];

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;
}
