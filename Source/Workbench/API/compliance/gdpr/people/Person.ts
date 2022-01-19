/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { PersonId } from './PersonId';
import { SocialSecurityNumber } from './SocialSecurityNumber';
import { FirstName } from './FirstName';
import { LastName } from './LastName';
import { Address } from './Address';
import { City } from './City';
import { PostalCode } from './PostalCode';
import { Country } from './Country';
import { PersonalInformation } from './PersonalInformation';

export type Person = {
    id: PersonId;
    socialSecurityNumber: SocialSecurityNumber;
    firstName: FirstName;
    lastName: LastName;
    address: Address;
    city: City;
    postalCode: PostalCode;
    country: Country;
    personalInformation: PersonalInformation[];
};
