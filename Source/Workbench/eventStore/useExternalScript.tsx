// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';

export const useExternalScript = (url) => {
    const [state, setState] = useState(url ? "loading" : "idle");

    useEffect(() => {
        if (!url) {
            setState("idle");
            return;
        }

        let script = document.querySelector(`script[src="${url}"]`);

        const handleScript = (e) => {
            setState(e.type === "load" ? "ready" : "error");
        };

        if (!script) {
            script = (document.createElement("script") as unknown as any)!;
            script.type = "application/javascript";
            script.src = url;
            script.async = true;
            document.body.appendChild(script);
            script.addEventListener("load", handleScript);
            script.addEventListener("error", handleScript);
        }

        script.addEventListener("load", handleScript);
        script.addEventListener("error", handleScript);

        return () => {
            script.removeEventListener("load", handleScript);
            script.removeEventListener("error", handleScript);
        };
    }, [url]);

    return state;
};
