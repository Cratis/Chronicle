// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

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
