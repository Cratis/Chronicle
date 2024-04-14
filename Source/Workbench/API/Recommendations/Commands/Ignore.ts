/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from 'Infrastructure/commands';
import { Validator } from 'Infrastructure/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/{namespace}/recommendations');

export interface IIgnore {
    eventStore?: string;
    namespace?: string;
    recommendationId?: string;
}

export class IgnoreValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        recommendationId: new Validator(),
    };
}

export class Ignore extends Command<IIgnore> implements IIgnore {
    readonly route: string = '/api/events/store/{eventStore}/{namespace}/recommendations';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new IgnoreValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _recommendationId!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
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
    get recommendationId(): string {
        return this._recommendationId;
    }

    set recommendationId(value: string) {
        this._recommendationId = value;
        this.propertyChanged('recommendationId');
    }

    static use(initialValues?: IIgnore): [Ignore, SetCommandValues<IIgnore>, ClearCommandValues] {
        return useCommand<Ignore, IIgnore>(Ignore, initialValues);
    }
}
