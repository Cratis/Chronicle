/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventType } from '../../Events/EventType';
import { WebhookTarget } from './WebhookTarget';

export class WebhookDefinition {

    @field(String)
    eventSequenceId!: string;

    @field(String)
    identifier!: string;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(WebhookTarget)
    target!: WebhookTarget;

    @field(Boolean)
    isReplayable!: boolean;

    @field(Boolean)
    isActive!: boolean;
}
