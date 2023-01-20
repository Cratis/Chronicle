/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import { EventTypeRegistration } from './EventTypeRegistration';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/types');

export interface IRegister {
    microserviceId?: string;
    types?: EventTypeRegistration[];
}

export class RegisterValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        types: new Validator(),
    };
}

export class Register extends Command<IRegister> implements IRegister {
    readonly route: string = '/api/events/store/{{microserviceId}}/types';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterValidator();

    private _microserviceId!: string;
    private _types!: EventTypeRegistration[];

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'types',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get types(): EventTypeRegistration[] {
        return this._types;
    }

    set types(value: EventTypeRegistration[]) {
        this._types = value;
        this.propertyChanged('types');
    }

    static use(initialValues?: IRegister): [Register, SetCommandValues<IRegister>, ClearCommandValues] {
        return useCommand<Register, IRegister>(Register, initialValues);
    }
}
