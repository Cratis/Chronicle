/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { IPropertyPathSegment } from './IPropertyPathSegment';

export class PropertyPath {

    @field(String)
    path!: string;

    @field(IPropertyPathSegment, true)
    segments!: IPropertyPathSegment[];

    @field(IPropertyPathSegment)
    lastSegment!: IPropertyPathSegment;

    @field(Boolean)
    isRoot!: boolean;

    @field(Boolean)
    isSet!: boolean;
}
