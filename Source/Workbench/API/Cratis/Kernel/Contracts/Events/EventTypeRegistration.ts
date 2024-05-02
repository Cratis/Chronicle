// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventType } from './EventType';

export class EventTypeRegistration {

    @field(EventType)
    type!: EventType;

    @field(String)
    friendlyName!: string;

    @field(String)
    schema!: string;
}
