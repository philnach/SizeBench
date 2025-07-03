#pragma warning disable IDE0005 // Using directive is unnecessary
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SizeBench.AnalysisEngine.PE;
#pragma warning restore IDE0005

namespace SizeBench.AnalysisEngine.Tests.PE;

[TestClass]
public sealed class MachineTypeTests
{
    [TestMethod]
    public void ARM64EC_MachineType_HasCorrectValue()
    {
        // Verify that ARM64EC has the correct machine type value
        Assert.AreEqual((ushort)0xA641, (ushort)MachineType.ARM64EC);
    }

    [TestMethod]
    public void ARM64EC_UsesCorrectRvaAdjustment()
    {
        // Verify that ARM64EC uses ARM64-style RVA adjustment (no thumb bit masking)
        // ARM32 masks the lowest bit, but ARM64 and ARM64EC should not
        var testRva = 0x12345679u; // RVA with lowest bit set
        
        // ARM32 should mask the lowest bit (thumb bit)
        var armAdjusted = EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM);
        Assert.AreEqual(0x12345678u, armAdjusted, "ARM32 should mask the thumb bit");
        
        // ARM64 and ARM64EC should not mask any bits
        var arm64Adjusted = EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM64);
        var arm64ecAdjusted = EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM64EC);
        
        Assert.AreEqual(testRva, arm64Adjusted, "ARM64 should not mask any bits");
        Assert.AreEqual(testRva, arm64ecAdjusted, "ARM64EC should not mask any bits (same as ARM64)");
        Assert.AreEqual(arm64Adjusted, arm64ecAdjusted, "ARM64EC should behave identically to ARM64");
    }
}
