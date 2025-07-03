# ARM64EC Support Implementation

## Summary
Added support for ARM64EC (ARM64 Emulation Compatible) binaries to SizeBench. ARM64EC is a 64-bit architecture that allows ARM64 binaries to interoperate with x64 code on ARM64 devices running Windows.

## Changes Made

### 1. Core Machine Type Support
- **File**: `src/SizeBench.AnalysisEngine/PE/PInvokes.cs`
  - Added `ARM64EC = 0xA641` to the `MachineType` enum

### 2. DIA Adapter Updates
- **File**: `src/SizeBench.AnalysisEngine/DIAInterop/DIAAdapter.cs`
  - Added `case 0xA641: // IMAGE_FILE_MACHINE_ARM64EC` to the supported machine types
  - Removed the previous exception that blocked ARM64EC binaries

### 3. Exception Handling Support
- **File**: `src/SizeBench.AnalysisEngine/PE/EHSymbolTable.cs`
  - Added `MachineType.ARM64EC` to the ARM/ARM64 case that uses `ARM_EHParser`
  - ARM64EC uses the same exception handling structures as ARM64

### 4. PE File Parsing
- **File**: `src/SizeBench.AnalysisEngine/PE/PEFile.cs`
  - Added `(Machine)0xA641 => MachineType.ARM64EC` mapping for PE header parsing
  - Used numeric constant since `Machine.Arm64EC` may not be available in current .NET version

### 5. Debugger Integration
- **File**: `src/SizeBench.AnalysisEngine/DebuggerInterop/DebuggerAdapter.cs`
  - Added `MachineType.ARM64EC => "ARM64EC"` mapping for debugger target architecture

### 6. Unit Tests
- **File**: `src/SizeBench.AnalysisEngine.Tests/PE/MachineTypeTests.cs`
  - Added test to verify ARM64EC machine type has correct value (0xA641)
  - Added basic validation test for ARM64EC support

## Technical Details

### Exception Handling Behavior
ARM64EC behaves like ARM64 for exception handling purposes:
- Uses the `ARM_EHParser` (same as ARM64)
- Does NOT use ARM32 Thumb mode RVA adjustments
- Uses ARM64 XDATA structure layout (22-bit field layout vs ARM32's 23-bit layout)

### Architecture Classification
ARM64EC is treated as a 64-bit ARM architecture:
- No Thumb bit masking (unlike ARM32)
- Uses ARM64 exception data structures
- Compatible with ARM64 unwinding mechanisms

## Testing
The implementation includes unit tests to verify:
1. Correct machine type constant value (0xA641)
2. Proper integration with existing ARM64 code paths

## Future Considerations
- When .NET adds native `Machine.Arm64EC` support, the numeric constant `(Machine)0xA641` can be replaced with the proper enum value
- ARM64EC binaries should be tested with real ARM64EC PE files when available
- Additional ARM64EC-specific features may require future updates

## Compatibility
This implementation maintains backward compatibility:
- All existing ARM32 and ARM64 functionality remains unchanged
- ARM64EC is treated as an extension of ARM64 support
- No breaking changes to existing APIs or behavior
