// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity.given;

public class windows_permissions : Specification
{
    protected const string CurrentUser = "S-1-5-21-100-200-300-1001";
    protected const string ForeignUser = "S-1-5-21-100-200-300-1002";
    protected const int FullControl = 0x001F01FF;
    protected const int ReadAndExecute = 0x000200A9;
    protected bool _canRead;
}
