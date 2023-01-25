/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { IPropertyPathSegment } from './IPropertyPathSegment';

export class PropertyPath {

    @field(String)
    path!: string;

    @field(String)
    path!: string;

    @field(Object, true, [
    ])
    segments!: IPropertyPathSegment[];

    @field(Object, false, [
    ])
    lastSegment!: IPropertyPathSegment;

    @field(Boolean)
    isRoot!: boolean;

    @field(Boolean)
    isSet!: boolean;
}
