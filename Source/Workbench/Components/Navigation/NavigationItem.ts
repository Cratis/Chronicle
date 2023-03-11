// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type NavigationItem = {
    title: string;
    icon: JSX.Element;
    path: string;
    content: JSX.Element;
    children?: NavigationItem[]
};
