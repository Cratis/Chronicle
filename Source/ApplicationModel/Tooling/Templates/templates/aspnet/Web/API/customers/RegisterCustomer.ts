/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/customers');

export interface IRegisterCustomer {
    customerId?: string;
    firstName?: string;
    lastName?: string;
    socialSecurityNumber?: string;
}

export class RegisterCustomerValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        customerId: new Validator(),
        firstName: new Validator(),
        lastName: new Validator(),
        socialSecurityNumber: new Validator(),
    };
}

export class RegisterCustomer extends Command<IRegisterCustomer> implements IRegisterCustomer {
    readonly route: string = '/api/customers';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterCustomerValidator();

    private _customerId!: string;
    private _firstName!: string;
    private _lastName!: string;
    private _socialSecurityNumber!: string;

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'customerId',
            'firstName',
            'lastName',
            'socialSecurityNumber',
        ];
    }

    get customerId(): string {
        return this._customerId;
    }

    set customerId(value: string) {
        this._customerId = value;
        this.propertyChanged('customerId');
    }
    get firstName(): string {
        return this._firstName;
    }

    set firstName(value: string) {
        this._firstName = value;
        this.propertyChanged('firstName');
    }
    get lastName(): string {
        return this._lastName;
    }

    set lastName(value: string) {
        this._lastName = value;
        this.propertyChanged('lastName');
    }
    get socialSecurityNumber(): string {
        return this._socialSecurityNumber;
    }

    set socialSecurityNumber(value: string) {
        this._socialSecurityNumber = value;
        this.propertyChanged('socialSecurityNumber');
    }

    static use(initialValues?: IRegisterCustomer): [RegisterCustomer, SetCommandValues<IRegisterCustomer>] {
        return useCommand<RegisterCustomer, IRegisterCustomer>(RegisterCustomer, initialValues);
    }
}
