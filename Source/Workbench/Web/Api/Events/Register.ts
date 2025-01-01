/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
/* eslint-disable @typescript-eslint/no-empty-interface */
// eslint-disable-next-line header/header
import { Command, CommandPropertyValidators, CommandValidator } from '@cratis/applications/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/applications.react/commands';
import { Validator } from '@cratis/applications/validation';
import { EventTypeRegistration } from './EventTypeRegistration';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStoreName}}/types');

export interface IRegister {
    eventStoreName?: string;
    types?: EventTypeRegistration[];
}

export class RegisterValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStoreName: new Validator(),
        types: new Validator(),
    };
}

export class Register extends Command<IRegister> implements IRegister {
    readonly route: string = '/api/event-store/{eventStoreName}/types';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterValidator();

    private _eventStoreName!: string;
    private _types!: EventTypeRegistration[];

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
            'types',
        ];
    }

    get eventStoreName(): string {
        return this._eventStoreName;
    }

    set eventStoreName(value: string) {
        this._eventStoreName = value;
        this.propertyChanged('eventStoreName');
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
