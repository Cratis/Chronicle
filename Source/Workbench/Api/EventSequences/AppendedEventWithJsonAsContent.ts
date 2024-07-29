/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventMetadata } from '../Chronicle/Events/EventMetadata';
import { EventType } from '../Chronicle/Events/EventType';
import { EventContext } from '../Chronicle/Events/EventContext';
import { Causation } from '../Chronicle/Auditing/Causation';
import { Identity } from '../Chronicle/Identities/Identity';
import { EventObservationState } from '../Chronicle/Events/EventObservationState';

export class AppendedEventWithJsonAsContent {

    @field(EventMetadata)
    metadata!: EventMetadata;

    @field(EventContext)
    context!: EventContext;

    @field(Object)
    content!: any;
}
