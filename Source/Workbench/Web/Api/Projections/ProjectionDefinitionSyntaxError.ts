import { field } from '@cratis/fundamentals';


export class ProjectionDefinitionSyntaxError {

    @field(String)
    message!: string;

    @field(Number)
    line!: number;

    @field(Number)
    column!: number;
}
