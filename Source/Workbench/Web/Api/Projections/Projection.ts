/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';

export class Projection {

    @field(Guid)
    id!: Guid;

    @field(Boolean)
    isActive!: boolean;

    @field(String)
    modelName!: string;
}
