// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#ifdef FEATURE_CACHED_INTERFACE_DISPATCH

    .extern RhpCidResolve
    .extern RhpUniversalTransition_DebugStepTailCall

//
// Cache miss case, call the runtime to resolve the target and update the cache.
// Use universal transition helper to allow an exception to flow out of resolution.
//
    LEAF_ENTRY RhpInterfaceDispatchSlow, _TEXT
        // x11 contains the interface dispatch cell address.
        // Calling convention of the universal thunk is:
        //  xip0: target address for the thunk to call
        //  xip1: parameter of the thunk's target
        PREPARE_EXTERNAL_VAR RhpCidResolve, xip0
        mov xip1, x11
        b       C_FUNC(RhpUniversalTransition_DebugStepTailCall)
    LEAF_END RhpInterfaceDispatchSlow, _TEXT

#endif // FEATURE_CACHED_INTERFACE_DISPATCH
