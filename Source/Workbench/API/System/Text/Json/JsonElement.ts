/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { JsonValueKind } from './JsonValueKind';
import { JsonElement } from './JsonElement';

export class JsonElement {

    @field(JsonValueKind)
    valueKind!: JsonValueKind;

    @field(JsonElement)
    item!: JsonElement;
}
