/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
/* eslint-disable @typescript-eslint/no-empty-interface */
// eslint-disable-next-line header/header
import { Command, CommandPropertyValidators, CommandValidator } from '@cratis/applications/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/applications.react/commands';
import { Validator } from '@cratis/applications/validation';
import { PropertyDescriptor } from '@cratis/applications/reflection';
import { Guid } from '@cratis/fundamentals';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/recommendations/{{recommendationId}}/ignore');

export interface IIgnore {
    eventStore?: string;
    namespace?: string;
    recommendationId?: Guid;
}

export class IgnoreValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        recommendationId: new Validator(),
    };
}

export class Ignore extends Command<IIgnore> implements IIgnore {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/recommendations/{recommendationId}/ignore';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new IgnoreValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('eventStore', String),
        new PropertyDescriptor('namespace', String),
        new PropertyDescriptor('recommendationId', Guid),
    ];

    private _eventStore!: string;
    private _namespace!: string;
    private _recommendationId!: Guid;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [
            'eventStore',
            'namespace',
            'recommendationId',
        ];
    }

    get properties(): string[] {
        return [
            'eventStore',
            'namespace',
            'recommendationId',
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
    get recommendationId(): Guid {
        return this._recommendationId;
    }

    set recommendationId(value: Guid) {
        this._recommendationId = value;
        this.propertyChanged('recommendationId');
    }

    static use(initialValues?: IIgnore): [Ignore, SetCommandValues<IIgnore>, ClearCommandValues] {
        return useCommand<Ignore, IIgnore>(Ignore, initialValues);
    }
}
