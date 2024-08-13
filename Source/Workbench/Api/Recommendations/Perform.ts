/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { Command, CommandValidator, CommandPropertyValidators } from '@cratis/applications/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/applications.react/commands';
import { Validator } from '@cratis/applications/validation';
import { Guid } from '@cratis/fundamentals';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/recommendations/{{recommendationId}}/perform');

export interface IPerform {
    eventStore?: string;
    namespace?: string;
    recommendationId?: Guid;
}

export class PerformValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        eventStore: new Validator(),
        namespace: new Validator(),
        recommendationId: new Validator(),
    };
}

export class Perform extends Command<IPerform> implements IPerform {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/recommendations/{recommendationId}/perform';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new PerformValidator();

    private _eventStore!: string;
    private _namespace!: string;
    private _recommendationId!: Guid;

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
    get recommendationId(): Guid {
        return this._recommendationId;
    }

    set recommendationId(value: Guid) {
        this._recommendationId = value;
        this.propertyChanged('recommendationId');
    }

    static use(initialValues?: IPerform): [Perform, SetCommandValues<IPerform>, ClearCommandValues] {
        return useCommand<Perform, IPerform>(Perform, initialValues);
    }
}
