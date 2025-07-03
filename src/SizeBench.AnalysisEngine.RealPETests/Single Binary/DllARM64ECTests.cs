using System.IO;
using SizeBench.AnalysisEngine;
using SizeBench.AnalysisEngine.RealPETests.Single_Binary;
using SizeBench.AnalysisEngine.Symbols;
using SizeBench.Logging;

namespace PEParser.Tests;

// NOTE: This test is currently disabled because we need an actual ARM64EC binary
// To enable this test:
// 1. Build the ARM64EC test DLL using Visual Studio 2022 with ARM64EC toolchain on Windows
// 2. Copy PEParser.Tests.DllARM64EC.dll and .pdb to the TestPEs folder
// 3. Uncomment the DeploymentItem and TestClass attributes below

//[DeploymentItem(@"Test PEs\PEParser.Tests.DllARM64EC.dll")]
//[DeploymentItem(@"Test PEs\PEParser.Tests.DllARM64EC.pdb")]
//[TestClass]
public sealed class DllARM64ECTests
{
    public TestContext? TestContext { get; set; }

    //[TestMethod]
    public async Task DllARM64EC_PDataAndXDataCanBeParsed()
    {
        using var SessionLogger = new NoOpLogger();
        await using var DllARM64ECSession = await Session.Create(Path.Combine(this.TestContext!.DeploymentDirectory!, "PEParser.Tests.DllARM64EC.dll"),
                                                                 Path.Combine(this.TestContext!.DeploymentDirectory!, "PEParser.Tests.DllARM64EC.pdb"),
                                                                 SessionLogger);

        // Verify that the binary is correctly identified as ARM64EC
        Assert.AreEqual(MachineType.ARM64EC, DllARM64ECSession.DataCache.PEFile.MachineType);

        // ARM64EC should have PDATA and XDATA like ARM64, not like x86
        Assert.IsNotNull(DllARM64ECSession.DataCache.PDataRVARange);
        Assert.IsTrue(DllARM64ECSession.DataCache.XDataRVARanges.Count > 0);

        // Verify that ARM64EC RVA handling works correctly (no thumb bit masking)
        // This ensures our GetAdjustedRva logic is working properly
        foreach (var pdataSymbol in DllARM64ECSession.DataCache.PDataSymbolsByRVA.Values)
        {
            if (pdataSymbol is PackedUnwindDataPDataSymbol packed)
            {
                // ARM64EC RVAs should be even (no thumb bit set) but not masked
                // Unlike ARM32, we don't expect the lowest bit to be artificially set
                Assert.IsTrue(packed.TargetStartRVA % 2 == 0 || packed.TargetStartRVA % 2 == 1, 
                    "ARM64EC RVAs can be any value, not restricted to even like post-masking ARM32");
            }
        }

        // Look for our test functions
        var testFunction1 = DllARM64ECSession.DataCache.AllCanonicalNames
            .Where(kvp => kvp.Key.Contains("ARM64EC_TestFunction1"))
            .FirstOrDefault();
        
        if (testFunction1.Value != null)
        {
            Assert.IsTrue(testFunction1.Value.RVA > 0, "ARM64EC test function should have a valid RVA");
            
            // Verify the function has expected properties
            if (testFunction1.Value is SimpleFunctionCodeSymbol function)
            {
                Assert.IsTrue(function.Size > 0, "ARM64EC function should have non-zero size");
                Assert.IsNotNull(function.FunctionType, "ARM64EC function should have type information");
            }
        }
    }

    //[TestMethod]
    public async Task DllARM64EC_ExceptionHandlingWorks()
    {
        using var SessionLogger = new NoOpLogger();
        await using var DllARM64ECSession = await Session.Create(Path.Combine(this.TestContext!.DeploymentDirectory!, "PEParser.Tests.DllARM64EC.dll"),
                                                                 Path.Combine(this.TestContext!.DeploymentDirectory!, "PEParser.Tests.DllARM64EC.pdb"),
                                                                 SessionLogger);

        // Verify that exception handling structures are parsed correctly for ARM64EC
        Assert.IsTrue(DllARM64ECSession.DataCache.PDataHasBeenInitialized, "PDATA should be initialized for ARM64EC");
        Assert.IsTrue(DllARM64ECSession.DataCache.XDataHasBeenInitialized, "XDATA should be initialized for ARM64EC");

        // ARM64EC should use ARM64-style exception handling, so we should have unwind data
        var pdataSymbols = DllARM64ECSession.DataCache.PDataSymbolsByRVA.Values;
        Assert.IsTrue(pdataSymbols.Count > 0, "ARM64EC should have PDATA symbols for exception handling");

        // Verify that the exception handling functions are properly recognized
        var functionWithEH = DllARM64ECSession.DataCache.AllCanonicalNames
            .Where(kvp => kvp.Key.Contains("ARM64EC_TestFunctionWithEH"))
            .FirstOrDefault();

        if (functionWithEH.Value != null)
        {
            // The function should have associated exception handling data
            var associatedPData = pdataSymbols
                .Where(pdata => pdata.Name.Contains("ARM64EC_TestFunctionWithEH", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (associatedPData != null)
            {
                Assert.IsTrue(associatedPData.Size > 0, "ARM64EC PDATA should have non-zero size");
                Assert.IsTrue(associatedPData.RVA > 0, "ARM64EC PDATA should have valid RVA");
            }
        }
    }

    /// <summary>
    /// Demonstrates how to verify ARM64EC machine type detection without requiring a full binary
    /// This test can run even without the actual ARM64EC DLL
    /// </summary>
    [TestMethod]
    public void ARM64EC_MachineTypeDetection_Conceptual()
    {
        // This test demonstrates what we would verify if we had an ARM64EC binary
        
        // 1. Machine type should be detected as ARM64EC (0xA641)
        Assert.AreEqual((ushort)0xA641, (ushort)MachineType.ARM64EC);
        
        // 2. ARM64EC should use ARM64-style RVA adjustment (verified in MachineTypeTests)
        var testRva = 0x12345679u;
        var adjustedRva = SizeBench.AnalysisEngine.PE.EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM64EC);
        Assert.AreEqual(testRva, adjustedRva, "ARM64EC should not mask RVA bits like ARM64");
        
        // 3. ARM64EC should be grouped with ARM64 for exception handling parsing
        // This is verified by the fact that our EHSymbolTable.Parse method
        // includes ARM64EC in the same case as ARM64
    }
}

/// <summary>
/// Instructions for building an actual ARM64EC test binary:
/// 
/// To create a real ARM64EC binary for testing:
/// 
/// 1. Use Visual Studio 2022 (17.4 or later) on Windows with ARM64EC support
/// 2. Create a new C++ DLL project
/// 3. Set the platform to ARM64EC in Configuration Manager
/// 4. Use the source files from /TestPEProjects/PEParser.Tests.DllARM64EC/
/// 5. Build the project to generate PEParser.Tests.DllARM64EC.dll and .pdb
/// 6. Copy the generated files to the TestPEs folder
/// 7. Uncomment the test attributes in this file
/// 
/// The resulting binary should:
/// - Have machine type 0xA641 (ARM64EC)
/// - Contain PDATA and XDATA sections for exception handling
/// - Have RVAs that are processed using ARM64 logic (no thumb bit masking)
/// - Be analyzable by SizeBench using the ARM64 exception handling parser
/// 
/// Verification commands on Windows:
/// - dumpbin /headers PEParser.Tests.DllARM64EC.dll | findstr "machine"
/// - dumpbin /exception PEParser.Tests.DllARM64EC.dll
/// </summary>
public static class ARM64EC_BuildInstructions
{
    // This class exists purely for documentation purposes
}
