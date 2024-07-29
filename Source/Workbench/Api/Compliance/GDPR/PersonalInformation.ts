/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';

export class PersonalInformation {

    @field(Guid)
    identifier!: Guid;

    @field(String)
    type!: string;

    @field(String)
    value!: string;
}
