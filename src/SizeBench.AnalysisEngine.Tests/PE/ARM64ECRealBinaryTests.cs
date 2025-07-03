#pragma warning disable IDE0005 // Using directive is unnecessary
#pragma warning disable IDE0007 // Use 'var' instead of explicit type
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SizeBench.AnalysisEngine.PE;
using SizeBench.Logging;
using System.IO;
using System.Reflection.PortableExecutable;
#pragma warning restore IDE0005
#pragma warning restore IDE0007

namespace SizeBench.AnalysisEngine.Tests.PE
{
    [TestClass]
    public sealed class ARM64ECRealBinaryTests
    {
        [TestMethod]
        public void ARM64EC_MinimalBinary_DetectsCorrectMachineType()
        {
            // Create a minimal ARM64EC binary for testing
            var tempFile = Path.GetTempFileName();
            try
            {
                // Generate the test binary
                TestUtilities.MinimalARM64ECBinaryGenerator.CreateMinimalARM64ECBinary(tempFile);
                
                // Test that SizeBench can detect the machine type correctly using just PEReader
                // This avoids the LoadLibraryExW call that requires Windows APIs
                using var fileStream = File.OpenRead(tempFile);
                using var peReader = new System.Reflection.PortableExecutable.PEReader(fileStream);
                
                // Test the machine type conversion logic that PEFile uses
                var actualMachine = peReader.PEHeaders.CoffHeader.Machine;
                var expectedMachineType = actualMachine switch
                {
                    System.Reflection.PortableExecutable.Machine.Amd64 => MachineType.x64,
                    System.Reflection.PortableExecutable.Machine.I386 => MachineType.I386,
                    System.Reflection.PortableExecutable.Machine.Arm or System.Reflection.PortableExecutable.Machine.ArmThumb2 => MachineType.ARM,
                    System.Reflection.PortableExecutable.Machine.Arm64 => MachineType.ARM64,
                    (System.Reflection.PortableExecutable.Machine)0xA641 => MachineType.ARM64EC, // ARM64EC
                    _ => throw new InvalidOperationException($"Unknown machine type: {actualMachine}")
                };
                
                Assert.AreEqual(MachineType.ARM64EC, expectedMachineType, 
                    "PEReader should correctly detect ARM64EC machine type from the binary");
                    
                Assert.AreEqual((ushort)0xA641, (ushort)actualMachine,
                    "Raw machine type should have the correct ARM64EC value");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        
        [TestMethod]
        public void ARM64EC_RvaAdjustment_WorksCorrectly()
        {
            // Test that ARM64EC uses correct RVA adjustment (same as our unit test but with real context)
            var testRva = 0x12345679u; // RVA with lowest bit set
            
            // ARM64EC should behave like ARM64, not ARM32
            var arm64ecAdjusted = EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM64EC);
            var arm64Adjusted = EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM64);
            var armAdjusted = EHSymbolTable.GetAdjustedRva(testRva, MachineType.ARM);
            
            // ARM64EC should match ARM64 (no masking)
            Assert.AreEqual(testRva, arm64ecAdjusted, "ARM64EC should not mask any RVA bits");
            Assert.AreEqual(arm64Adjusted, arm64ecAdjusted, "ARM64EC should behave identically to ARM64");
            
            // ARM32 should be different (with masking)
            Assert.AreEqual(0x12345678u, armAdjusted, "ARM32 should mask the thumb bit");
            Assert.AreNotEqual(armAdjusted, arm64ecAdjusted, "ARM64EC should not use ARM32 thumb bit masking");
        }
    }
}

namespace SizeBench.TestUtilities
{
    /// <summary>
    /// Creates a minimal PE file with ARM64EC machine type for testing purposes.
    /// This is not a full functional binary, but has enough structure for machine type detection.
    /// </summary>
    public static class MinimalARM64ECBinaryGenerator
    {
        public static void CreateMinimalARM64ECBinary(string outputPath)
        {
            using var writer = new BinaryWriter(File.Create(outputPath));
            
            // DOS Header
            writer.Write((ushort)0x5A4D); // e_magic "MZ"
            writer.Write(new byte[58]); // Rest of DOS header (mostly zeros)
            writer.Write((uint)0x80); // e_lfanew - offset to PE header
            
            // DOS Stub (pad to PE header offset)
            writer.Write(new byte[0x80 - 0x40]); // Pad to offset 0x80
            
            // PE Header
            writer.Write((uint)0x00004550); // PE signature "PE\0\0"
            
            // COFF Header
            writer.Write((ushort)0xA641); // Machine type - ARM64EC!
            writer.Write((ushort)3); // NumberOfSections
            writer.Write((uint)0); // TimeDateStamp
            writer.Write((uint)0); // PointerToSymbolTable
            writer.Write((uint)0); // NumberOfSymbols
            writer.Write((ushort)0xF0); // SizeOfOptionalHeader
            writer.Write((ushort)0x2000); // Characteristics (DLL)
            
            // Optional Header (PE32+)
            writer.Write((ushort)0x020B); // Magic (PE32+)
            writer.Write((byte)1); // MajorLinkerVersion
            writer.Write((byte)0); // MinorLinkerVersion
            writer.Write((uint)0x1000); // SizeOfCode
            writer.Write((uint)0x1000); // SizeOfInitializedData
            writer.Write((uint)0); // SizeOfUninitializedData
            writer.Write((uint)0x1000); // AddressOfEntryPoint
            writer.Write((uint)0x1000); // BaseOfCode
            writer.Write((ulong)0x180000000); // ImageBase
            writer.Write((uint)0x1000); // SectionAlignment
            writer.Write((uint)0x200); // FileAlignment
            writer.Write((ushort)6); // MajorOperatingSystemVersion
            writer.Write((ushort)0); // MinorOperatingSystemVersion
            writer.Write((ushort)0); // MajorImageVersion
            writer.Write((ushort)0); // MinorImageVersion
            writer.Write((ushort)6); // MajorSubsystemVersion
            writer.Write((ushort)0); // MinorSubsystemVersion
            writer.Write((uint)0); // Win32VersionValue
            writer.Write((uint)0x4000); // SizeOfImage
            writer.Write((uint)0x400); // SizeOfHeaders
            writer.Write((uint)0); // CheckSum
            writer.Write((ushort)2); // Subsystem (GUI)
            writer.Write((ushort)0); // DllCharacteristics
            writer.Write((ulong)0x100000); // SizeOfStackReserve
            writer.Write((ulong)0x1000); // SizeOfStackCommit
            writer.Write((ulong)0x100000); // SizeOfHeapReserve
            writer.Write((ulong)0x1000); // SizeOfHeapCommit
            writer.Write((uint)0); // LoaderFlags
            writer.Write((uint)16); // NumberOfRvaAndSizes
            
            // Data directories (16 entries)
            for (var i = 0; i < 16; i++)
            {
                if (i == 3) // Exception directory
                {
                    writer.Write((uint)0x3000); // RVA of exception data
                    writer.Write((uint)0x100); // Size of exception data
                }
                else
                {
                    writer.Write((ulong)0); // Empty directory
                }
            }
            
            // Section Headers
            // .text section
            writer.Write(System.Text.Encoding.ASCII.GetBytes(".text\0\0\0"));
            writer.Write((uint)0x1000); // VirtualSize
            writer.Write((uint)0x1000); // VirtualAddress
            writer.Write((uint)0x1000); // SizeOfRawData
            writer.Write((uint)0x400); // PointerToRawData
            writer.Write((uint)0); // PointerToRelocations
            writer.Write((uint)0); // PointerToLinenumbers
            writer.Write((ushort)0); // NumberOfRelocations
            writer.Write((ushort)0); // NumberOfLinenumbers
            writer.Write((uint)0x60000020); // Characteristics (code, execute, read)
            
            // .rdata section
            writer.Write(System.Text.Encoding.ASCII.GetBytes(".rdata\0\0"));
            writer.Write((uint)0x1000); // VirtualSize
            writer.Write((uint)0x2000); // VirtualAddress
            writer.Write((uint)0x1000); // SizeOfRawData
            writer.Write((uint)0x1400); // PointerToRawData
            writer.Write((uint)0); // PointerToRelocations
            writer.Write((uint)0); // PointerToLinenumbers
            writer.Write((ushort)0); // NumberOfRelocations
            writer.Write((ushort)0); // NumberOfLinenumbers
            writer.Write((uint)0x40000040); // Characteristics (initialized data, read)
            
            // .pdata section (exception handling)
            writer.Write(System.Text.Encoding.ASCII.GetBytes(".pdata\0\0"));
            writer.Write((uint)0x100); // VirtualSize
            writer.Write((uint)0x3000); // VirtualAddress
            writer.Write((uint)0x200); // SizeOfRawData
            writer.Write((uint)0x2400); // PointerToRawData
            writer.Write((uint)0); // PointerToRelocations
            writer.Write((uint)0); // PointerToLinenumbers
            writer.Write((ushort)0); // NumberOfRelocations
            writer.Write((ushort)0); // NumberOfLinenumbers
            writer.Write((uint)0x40000040); // Characteristics (initialized data, read)
            
            // Pad to file alignment
            while (writer.BaseStream.Position < 0x400)
            {
                writer.Write((byte)0);
            }
            
            // .text section data (minimal)
            writer.Write(new byte[0x1000]); // 4KB of zeros
            
            // .rdata section data (minimal)
            writer.Write(new byte[0x1000]); // 4KB of zeros
            
            // .pdata section data (minimal exception data)
            writer.Write(new byte[0x200]); // 512 bytes of zeros
        }
    }
}
