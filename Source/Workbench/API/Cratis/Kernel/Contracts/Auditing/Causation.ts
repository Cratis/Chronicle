// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class Causation {

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(String)
    type!: string;

    @field(Object)
    properties!: any;
}
