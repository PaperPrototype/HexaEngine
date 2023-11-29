﻿namespace D3D12Testing.Graphics
{
    public enum ResultCode
    {
        DXGI_ERROR_ACCESS_DENIED = unchecked((int)0x887A002B),
        DXGI_ERROR_ACCESS_LOST = unchecked((int)0x887A0026),
        DXGI_ERROR_ALREADY_EXISTS = unchecked((int)0x887A0036),
        DXGI_ERROR_CANNOT_PROTECT_CONTENT = unchecked((int)0x887A002A),
        DXGI_ERROR_DEVICE_HUNG = unchecked((int)0x887A0006),
        DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005),
        DXGI_ERROR_DEVICE_RESET = unchecked((int)0x887A0007),
        DXGI_ERROR_DRIVER_INTERNAL_ERROR = unchecked((int)0x887A0020),
        DXGI_ERROR_FRAME_STATISTICS_DISJOINT = unchecked((int)0x887A000B),
        DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE = unchecked((int)0x887A000C),
        DXGI_ERROR_INVALID_CALL = unchecked((int)0x887A0001),
        DXGI_ERROR_MORE_DATA = unchecked((int)0x887A0003),
        DXGI_ERROR_NAME_ALREADY_EXISTS = unchecked((int)0x887A002C),
        DXGI_ERROR_NONEXCLUSIVE = unchecked((int)0x887A0021),
        DXGI_ERROR_NOT_CURRENTLY_AVAILABLE = unchecked((int)0x887A0022),
        DXGI_ERROR_NOT_FOUND = unchecked((int)0x887A0002),
        DXGI_ERROR_REMOTE_CLIENT_DISCONNECTED = unchecked((int)0x887A0023),
        DXGI_ERROR_REMOTE_OUTOFMEMORY = unchecked((int)0x887A0024),
        DXGI_ERROR_RESTRICT_TO_OUTPUT_STALE = unchecked((int)0x887A0029),
        DXGI_ERROR_SDK_COMPONENT_MISSING = unchecked((int)0x887A002D),
        DXGI_ERROR_SESSION_DISCONNECTED = unchecked((int)0x887A0028),
        DXGI_ERROR_UNSUPPORTED = unchecked((int)0x887A0004),
        DXGI_ERROR_WAIT_TIMEOUT = unchecked((int)0x887A0027),
        DXGI_ERROR_WAS_STILL_DRAWING = unchecked((int)0x887A000A),

        D3D12_ERROR_ADAPTER_NOT_FOUND = unchecked((int)0x887e0001),
        D3D12_ERROR_DRIVER_VERSION_MISMATCH = unchecked((int)0x887e0002),

        E_FAIL = unchecked((int)0x80004005),
        E_INVALIDARG = unchecked((int)0x80070057),
        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        E_NOTIMPL = unchecked((int)0x80004001),
        S_FALSE = unchecked(1),
        S_OK = unchecked(0),
    }
}