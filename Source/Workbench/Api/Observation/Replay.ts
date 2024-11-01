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

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/observers/{{namespace}}/replay/{{observerId}}');

export interface IReplay {
    eventStore?: string;
    namespace?: string;
    observerId?: string;
}

export class ReplayValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        observerId: new Validator(),
    };
}

export class Replay extends Command<IReplay> implements IReplay {
    readonly route: string = '/api/event-store/{eventStore}/observers/{namespace}/replay/{observerId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ReplayValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _observerId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
            'observerId',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'namespace',
            'observerId',
        ];
    }

    get eventStore(): string {
        return this._eventStore;
    }

    set eventStore(value: string) {
        this._eventStore = value;
        this.propertyChanged('eventStore');
    }
    get namespace(): string {
        return this._namespace;
    }

    set namespace(value: string) {
        this._namespace = value;
        this.propertyChanged('namespace');
    }
    get observerId(): string {
        return this._observerId;
    }

    set observerId(value: string) {
        this._observerId = value;
        this.propertyChanged('observerId');
    }

    static use(initialValues?: IReplay): [Replay, SetCommandValues<IReplay>, ClearCommandValues] {
        return useCommand<Replay, IReplay>(Replay, initialValues);
    }
}
