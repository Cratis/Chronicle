// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { INamespace } from "./NamespaceSelector";
import css from "./NamespaceSelector.module.css";

interface INamespaceListItemProps {
    namespace: INamespace;
    onClick: () => void;
}

export const NamespaceListItem = ({ namespace: namespace, onClick }: INamespaceListItemProps) => {
    return <li onClick={onClick} className={`p-2 ${css.namespaceListItem}`}>
        {namespace.name}
    </li>;
}
