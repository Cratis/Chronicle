/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { PropertyPath } from './PropertyPath';
import { IPropertyPathSegment } from './IPropertyPathSegment';

export class ArrayIndexer {

    @field(PropertyPath)
    arrayProperty!: PropertyPath;

    @field(PropertyPath)
    identifierProperty!: PropertyPath;

    @field(Object)
    identifier!: any;
}
