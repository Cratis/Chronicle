/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { PersonalInformation } from './PersonalInformation';

export class Person {

    @field(String)
    id!: string;

    @field(String)
    socialSecurityNumber!: string;

    @field(String)
    firstName!: string;

    @field(String)
    lastName!: string;

    @field(String)
    address!: string;

    @field(String)
    city!: string;

    @field(String)
    postalCode!: string;

    @field(String)
    country!: string;

    @field(PersonalInformation, true)
    personalInformation!: PersonalInformation[];
}
