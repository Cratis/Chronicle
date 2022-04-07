/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/projections/{{projectionId}}/rewind');

export interface IRewind {
    projectionId?: string;
}

export class RewindValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        projectionId: new Validator(),
    };
}

export class Rewind extends Command<IRewind> implements IRewind {
    readonly route: string = '/api/events/projections/{{projectionId}}/rewind';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RewindValidator();

    private _projectionId!: string;

    get requestArguments(): string[] {
        return [
            'projectionId',
        ];
    }

    get properties(): string[] {
        return [
            'projectionId',
        ];
    }

    get projectionId(): string {
        return this._projectionId;
    }

    set projectionId(value: string) {
        this._projectionId = value;
        this.propertyChanged('projectionId');
    }

    static use(initialValues?: IRewind): [Rewind, SetCommandValues<IRewind>] {
        return useCommand<Rewind, IRewind>(Rewind, initialValues);
    }
}
