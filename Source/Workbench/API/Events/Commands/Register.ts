/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from 'Infrastructure/commands';
import { Validator } from 'Infrastructure/validation';
import { RegisterEventTypes } from './RegisterEventTypes';
import { EventTypeRegistration } from '../../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventTypeRegistration';
import { EventType } from '../../Users/einari/Projects/Cratis/Cratis/Source/Tools/ProxyGenerator/Cratis/Kernel/Contracts/Events/EventType';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStoreName}/types');

export interface IRegister {
    eventStoreName?: string;
    payload?: RegisterEventTypes;
}

export class RegisterValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStoreName: new Validator(),
        payload: new Validator(),
    };
}

export class Register extends Command<IRegister> implements IRegister {
    readonly route: string = '/api/events/store/{eventStoreName}/types';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterValidator();

    private _eventStoreName!: string;
    private _payload!: RegisterEventTypes;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStoreName',
        ];
    }

    get properties(): string[] {
        return [
            'eventStoreName',
            'payload',
        ];
    }

    get eventStoreName(): string {
        return this._eventStoreName;
    }

    set eventStoreName(value: string) {
        this._eventStoreName = value;
        this.propertyChanged('eventStoreName');
    }
    get payload(): RegisterEventTypes {
        return this._payload;
    }

    set payload(value: RegisterEventTypes) {
        this._payload = value;
        this.propertyChanged('payload');
    }

    static use(initialValues?: IRegister): [Register, SetCommandValues<IRegister>, ClearCommandValues] {
        return useCommand<Register, IRegister>(Register, initialValues);
    }
}
