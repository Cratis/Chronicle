declare namespace EventHistogramModuleScssNamespace {
    export interface IEventHistogramModuleScss {
        commandBar: string;
        container: string;
        eventListContainer: string;
        eventListPaging: string;
        eventSamplesContainer: string;
    }
}

declare const EventHistogramModuleScssModule: EventHistogramModuleScssNamespace.IEventHistogramModuleScss & {
    /** WARNING: Only available when `css-loader` is used without `style-loader` or `mini-css-extract-plugin` */
    locals: EventHistogramModuleScssNamespace.IEventHistogramModuleScss;
};

export = EventHistogramModuleScssModule;
