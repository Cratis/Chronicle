// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type NavigationItem = {
    title: string;
    icon: JSX.Element;
    targetPath: string;
    routePath?: string;
    content?: JSX.Element;
    children?: NavigationItem[],
    indexPage?: boolean;
};
