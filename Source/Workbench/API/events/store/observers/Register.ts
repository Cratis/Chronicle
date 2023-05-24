/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/applications/commands';
import { Validator } from '@aksio/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/observers/register/{{connectionId}}');

export interface IRegister {
    microserviceId?: string;
    connectionId?: string;
}

export class RegisterValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        connectionId: new Validator(),
    };
}

export class Register extends Command<IRegister> implements IRegister {
    readonly route: string = '/api/events/store/{{microserviceId}}/observers/register/{{connectionId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterValidator();

    private _microserviceId!: string;
    private _connectionId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'connectionId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'connectionId',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get connectionId(): string {
        return this._connectionId;
    }

    set connectionId(value: string) {
        this._connectionId = value;
        this.propertyChanged('connectionId');
    }

    static use(initialValues?: IRegister): [Register, SetCommandValues<IRegister>, ClearCommandValues] {
        return useCommand<Register, IRegister>(Register, initialValues);
    }
}
