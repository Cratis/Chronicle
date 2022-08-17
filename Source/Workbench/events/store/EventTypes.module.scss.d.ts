declare namespace EventTypesModuleScssNamespace {
    export interface IEventTypesModuleScss {
        container: string;
        eventDetails: string;
        eventList: string;
    }
}

declare const EventTypesModuleScssModule: EventTypesModuleScssNamespace.IEventTypesModuleScss & {
    /** WARNING: Only available when `css-loader` is used without `style-loader` or `mini-css-extract-plugin` */
    locals: EventTypesModuleScssNamespace.IEventTypesModuleScss;
};

export = EventTypesModuleScssModule;
