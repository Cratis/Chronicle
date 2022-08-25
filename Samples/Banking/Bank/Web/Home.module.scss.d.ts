declare namespace HomeModuleScssNamespace {
    export interface IHomeModuleScss {
        home: string;
    }
}

declare const HomeModuleScssModule: HomeModuleScssNamespace.IHomeModuleScss & {
    /** WARNING: Only available when `css-loader` is used without `style-loader` or `mini-css-extract-plugin` */
    locals: HomeModuleScssNamespace.IHomeModuleScss;
};

export = HomeModuleScssModule;
