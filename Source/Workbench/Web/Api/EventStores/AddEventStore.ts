/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
/* eslint-disable @typescript-eslint/no-empty-interface */
// eslint-disable-next-line header/header
import { Command, CommandPropertyValidators, CommandValidator } from '@cratis/applications/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/applications.react/commands';
import { Validator } from '@cratis/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-stores/add');

export interface IAddEventStore {
    name?: string;
}

export class AddEventStoreValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        name: new Validator(),
    };
}

export class AddEventStore extends Command<IAddEventStore> implements IAddEventStore {
    readonly route: string = '/api/event-stores/add';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddEventStoreValidator();

    private _name!: string;

    constructor() {
        super(Object, false);
    }

    get requestParameter(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'name',
        ];
    }

    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
        this.propertyChanged('name');
    }

    static use(initialValues?: IAddEventStore): [AddEventStore, SetCommandValues<IAddEventStore>, ClearCommandValues] {
        return useCommand<AddEventStore, IAddEventStore>(AddEventStore, initialValues);
    }
}
