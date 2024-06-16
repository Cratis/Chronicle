# Client

Configuration of the Cratis client happens through a file called `chronicle.json`.
It will look for the file sitting next to it in the same folder or within a folder called `config`.

If you don't provide a file, it will go with the default settings, which will typically be good for
local development of a default configured Kernel.

The file contains a top level set of keys, these keys represent a section each.

| Topic | Description |
| ------- | ----------- |
| [Kernel](./kernel.md) | Documentation how to configure cluster modes for Kernel and client. |
