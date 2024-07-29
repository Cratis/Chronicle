/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';

export class RecommendationInformation {

    @field(Guid)
    id!: Guid;

    @field(String)
    name!: string;

    @field(String)
    description!: string;

    @field(String)
    type!: string;

    @field(Date)
    occurred!: Date;
}
