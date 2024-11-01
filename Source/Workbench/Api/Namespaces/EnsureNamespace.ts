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

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/namespaces');

export interface IEnsureNamespace {
    eventStore?: string;
    name?: string;
}

export class EnsureNamespaceValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        name: new Validator(),
    };
}

export class EnsureNamespace extends Command<IEnsureNamespace> implements IEnsureNamespace {
    readonly route: string = '/api/event-store/{eventStore}/namespaces';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new EnsureNamespaceValidator();

    private _eventStore!: string;
    private _name!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'name',
        ];
    }

    get eventStore(): string {
        return this._eventStore;
    }

    set eventStore(value: string) {
        this._eventStore = value;
        this.propertyChanged('eventStore');
    }
    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
        this.propertyChanged('name');
    }

    static use(initialValues?: IEnsureNamespace): [EnsureNamespace, SetCommandValues<IEnsureNamespace>, ClearCommandValues] {
        return useCommand<EnsureNamespace, IEnsureNamespace>(EnsureNamespace, initialValues);
    }
}
