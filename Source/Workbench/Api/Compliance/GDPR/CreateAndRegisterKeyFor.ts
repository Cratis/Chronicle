/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { Command, CommandValidator, CommandPropertyValidators } from '@cratis/applications/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/applications.react/commands';
import { Validator } from '@cratis/applications/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/pii');

export interface ICreateAndRegisterKeyFor {
    identifier?: string;
}

export class CreateAndRegisterKeyForValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        identifier: new Validator(),
    };
}

export class CreateAndRegisterKeyFor extends Command<ICreateAndRegisterKeyFor> implements ICreateAndRegisterKeyFor {
    readonly route: string = '/api/compliance/gdpr/pii';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new CreateAndRegisterKeyForValidator();

    private _identifier!: string;

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'identifier',
        ];
    }

    get identifier(): string {
        return this._identifier;
    }

    set identifier(value: string) {
        this._identifier = value;
        this.propertyChanged('identifier');
    }

    static use(initialValues?: ICreateAndRegisterKeyFor): [CreateAndRegisterKeyFor, SetCommandValues<ICreateAndRegisterKeyFor>, ClearCommandValues] {
        return useCommand<CreateAndRegisterKeyFor, ICreateAndRegisterKeyFor>(CreateAndRegisterKeyFor, initialValues);
    }
}
