// PEParser.Tests.DllARM64EC.cpp : Defines the exported functions for the ARM64EC DLL application.
//

#include "stdafx.h"
#include <exception>

bool DllARM64EC_CppxdataUsage::MaybeThrow()
{
    try
    {
        if (m_bShouldThrow)
        {
            throw std::exception("ARM64EC dummy exception");
        }
    }
    catch (std::exception& except)
    {
        // Force the exception variable to be used to prevent optimization
        printf("Caught exception: %s\n", except.what());
        return false;
    }

    return true;
}

bool DllARM64EC_CppxdataUsage::MaybeThrowWithSEH()
{
    char a[1] = { '0' };

    __try
    {
        __try
        {
            // Force a potential access violation for SEH testing
            if (m_bShouldThrow)
            {
                char* p = nullptr;
                *p = 'X'; // This would cause an access violation
            }
        }
        __except(EXCEPTION_EXECUTE_HANDLER)
        {
            printf("ARM64EC SEH inner exception handled\n");
            return false;
        }
    }
    __except(EXCEPTION_EXECUTE_HANDLER)
    {
        printf("ARM64EC SEH outer exception handled\n");
        return false;
    }

    printf("ARM64EC SEH no exception: %c\n", a[0]);
    return true;
}

bool DllARM64EC_CppxdataUsage::MaybeThrowNested()
{
    try
    {
        try
        {
            if (m_bShouldThrow)
            {
                throw std::runtime_error("ARM64EC nested exception");
            }
        }
        catch (std::runtime_error& inner)
        {
            printf("ARM64EC inner catch: %s\n", inner.what());
            throw std::exception("ARM64EC re-throw from nested");
        }
    }
    catch (std::exception& outer)
    {
        printf("ARM64EC outer catch: %s\n", outer.what());
        return false;
    }

    return true;
}

// Exported functions for ARM64EC testing
extern "C" __declspec(dllexport) void ARM64EC_TestFunction1()
{
    // Simple function to generate ARM64EC code with specific RVA
    printf("ARM64EC Test Function 1 called\n");
    
    // Force some computation to ensure the function has substance
    volatile int sum = 0;
    for (int i = 0; i < 100; i++)
    {
        sum += i;
    }
    printf("ARM64EC computation result: %d\n", sum);
}

extern "C" __declspec(dllexport) void ARM64EC_TestFunction2()
{
    // Another simple function with different RVA
    printf("ARM64EC Test Function 2 called\n");
    
    // Different computation pattern
    volatile double result = 1.0;
    for (int i = 1; i <= 10; i++)
    {
        result *= (double)i;
    }
    printf("ARM64EC factorial result: %f\n", result);
}

extern "C" __declspec(dllexport) int ARM64EC_TestFunctionWithEH()
{
    // Function that demonstrates exception handling in ARM64EC
    DllARM64EC_CppxdataUsage testObject;
    
    try
    {
        if (!testObject.MaybeThrow())
        {
            printf("ARM64EC exception was caught and handled\n");
            return 1;
        }
        
        if (!testObject.MaybeThrowNested())
        {
            printf("ARM64EC nested exception was caught and handled\n");
            return 2;
        }
        
        return 0; // Success case
    }
    catch (...)
    {
        printf("ARM64EC unexpected exception in test function\n");
        return -1;
    }
}

// Additional ARM64EC specific test functions with various signatures
extern "C" __declspec(dllexport) long long ARM64EC_TestLongLong(long long a, long long b)
{
    return a * b + (a ^ b);
}

extern "C" __declspec(dllexport) double ARM64EC_TestDouble(double x, double y)
{
    return x * x + y * y;
}

extern "C" __declspec(dllexport) void* ARM64EC_TestPointer(void* ptr)
{
    // Test pointer handling in ARM64EC
    if (ptr)
    {
        printf("ARM64EC pointer test: %p\n", ptr);
    }
    return ptr;
}
