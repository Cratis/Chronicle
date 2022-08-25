declare namespace NavigationModuleScssNamespace {
    export interface INavigationModuleScss {
        navigationContainer: string;
    }
}

declare const NavigationModuleScssModule: NavigationModuleScssNamespace.INavigationModuleScss & {
    /** WARNING: Only available when `css-loader` is used without `style-loader` or `mini-css-extract-plugin` */
    locals: NavigationModuleScssNamespace.INavigationModuleScss;
};

export = NavigationModuleScssModule;
