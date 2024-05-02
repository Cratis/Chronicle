// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { Identity } from './Identity';

export class Identity {

    @field(String)
    subject!: string;

    @field(String)
    name!: string;

    @field(String)
    userName!: string;

    @field(Identity)
    onBehalfOf!: Identity;
}
