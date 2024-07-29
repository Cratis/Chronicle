/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
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
