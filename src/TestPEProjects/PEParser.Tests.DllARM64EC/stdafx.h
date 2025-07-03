#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files:
#include <windows.h>

// ARM64EC Test Classes for Exception Handling
class DllARM64EC_CppxdataUsage
{
public:
    bool MaybeThrow();
    bool MaybeThrowWithSEH();
    bool MaybeThrowNested();

private:
    bool m_bShouldThrow = true;
};

// Functions to ensure ARM64EC RVAs are generated correctly
extern "C" __declspec(dllexport) void ARM64EC_TestFunction1();
extern "C" __declspec(dllexport) void ARM64EC_TestFunction2();
extern "C" __declspec(dllexport) int ARM64EC_TestFunctionWithEH();
