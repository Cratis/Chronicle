// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** @type {import('tailwindcss').Config} */
module.exports = {
    content: {
        files: [
            "./index.html",
            "./Components/**/*.tsx",
            "./Features/**/*.tsx",
            "./Layout/**/*.tsx",
            "./node_modules/primereact/**/*.{js,ts,jsx,tsx}"
        ]
    },
    theme: {
        extend: {},
    },
    plugins: [],
}
