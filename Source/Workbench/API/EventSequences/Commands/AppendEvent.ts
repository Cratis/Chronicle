/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class AppendEvent {

    @field(String)
    eventSourceId!: string;

    @field(EventType)
    eventType!: EventType;

    @field(KeyValuePair`2, true)
    content!: KeyValuePair`2[];

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;

    @field(Nullable`1)
    validFrom?: Nullable`1;
}
