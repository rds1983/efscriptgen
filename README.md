# efscriptgen

A command-line tool that automatically generates batch compilation scripts for MonoGame and FNA effect files (.fx) with support for shader variants and preprocessor definitions. Perfect for managing complex shader pipelines with multiple feature combinations.

## Overview

**efscriptgen** streamlines the process of compiling HLSL effect files for game development frameworks. Instead of manually creating compilation scripts for each shader variant combination, efscriptgen:

- Discovers all `.fx` (effect) files in a directory tree
- Parses optional XML variant definition files
- Generates a Cartesian product of all variant combinations
- Creates framework-specific batch scripts (MonoGame DirectX11, MonoGame OpenGL, FNA)
- Outputs ready-to-run `.bat` files for compilation

This is particularly useful when you need to compile the same shader multiple times with different preprocessor flags to support features like:
- Texture vs non-textured materials
- Different lighting models
- Various shadow quality levels
- Platform-specific optimizations

## Installation & Update

### Install as Global Tool

```bash
dotnet tool install --global efscriptgen
```

After installation, the `efscriptgen` command will be available globally.

### Update to Latest Version

```bash
dotnet tool update --global efscriptgen
```

### Verify Installation

```bash
efscriptgen
```

## Usage & Output Structure

### Basic Usage

```bash
efscriptgen <folder> [options]
```

**Parameters:**
- `<folder>` - Path to the directory containing `.fx` effect files (scanned recursively)

**Options:**
- `-e <extension>` - Specifies the compiled output file extension (default: `efb`)

### Example

```bash
efscriptgen "C:\MyProject\Effects"
```

### Output Structure

For an input folder structure like:

```
MyEffects/
├── Basic.fx
├── Advanced.fx
├── Advanced.xml
└── lights/
    ├── Deferred.fx
    └── Deferred.xml
```

**efscriptgen** generates three output directory trees:

```
MyEffects/
├── MonoGameDX11/
│   └── bin/
│       ├── compile_Basic.bat
│       ├── compile_Advanced.bat
│       ├── lights/compile_Deferred.bat
│       └── compile_all.bat
├── MonoGameOGL/
│   └── bin/
│       ├── compile_Basic.bat
│       ├── compile_Advanced.bat
│       ├── lights/compile_Deferred.bat
│       └── compile_all.bat
└── FNA/
    └── bin/
        ├── compile_Basic.bat
        ├── compile_Advanced.bat
        ├── lights/compile_Deferred.bat
        └── compile_all.bat
```

**Special Scripts:**
- `compile_Basic.bat` - Compiles all variants of `Basic.fx`
- `compile_all.bat` - Runs all compilation scripts at once

### Batch Script Content Example

For a shader with variants, a generated batch script looks like:

```batch
mgfxc "C:\MyProject\Effects\Basic.fx" "C:\MyProject\Effects\MonoGameDX11\bin\Basic.efb" /Profile:DirectX_11
@if %errorlevel% neq 0 exit /b %errorlevel%
mgfxc "C:\MyProject\Effects\Basic.fx" "C:\MyProject\Effects\MonoGameDX11\bin\Basic_TEXTURE.efb" /Profile:DirectX_11 /Defines:TEXTURE=1
@if %errorlevel% neq 0 exit /b %errorlevel%
mgfxc "C:\MyProject\Effects\Basic.fx" "C:\MyProject\Effects\MonoGameDX11\bin\Basic_LIGHTNING.efb" /Profile:DirectX_11 /Defines:LIGHTNING=1
@if %errorlevel% neq 0 exit /b %errorlevel%
```

## Basic Shader Variants - Example

The simplest variant configuration uses Boolean flags to enable/disable shader features:

```xml
<Root>
	<MultiCompile>TEXTURE;_</MultiCompile>
	<MultiCompile>LIGHTNING;_</MultiCompile>
</Root>
```

**Explanation:**
- Each `<MultiCompile>` element represents one binary choice
- The semicolon (`;`) separates the options
- `TEXTURE` enables the flag (defines as `TEXTURE=1`)
- `_` represents "no define" (empty variant)

**Generated Variants (4 total):**
1. No defines
2. `TEXTURE=1`
3. `LIGHTNING=1`
4. `TEXTURE=1; LIGHTNING=1`

**Use Cases:**
- Textured vs untextured materials
- Lit vs unlit rendering
- Diffuse vs specular lighting
- Alpha blending vs opaque rendering

## Macros with Values - Example

For more complex configurations, variants can include preprocessor macros with specific values:

```xml
<Root>
	<MultiCompile>QUALITY=LOW;QUALITY=MEDIUM;QUALITY=HIGH</MultiCompile>
	<MultiCompile>TEXTURE;_</MultiCompile>
</Root>
```

**Explanation:**
- `QUALITY=LOW` - Defines `QUALITY` macro with value `LOW`
- Multiple key-value pairs separated by semicolons create alternatives
- The second `<MultiCompile>` still uses Boolean variants

**Generated Variants (6 total):**
1. `QUALITY=LOW`
2. `QUALITY=LOW; TEXTURE=1`
3. `QUALITY=MEDIUM`
4. `QUALITY=MEDIUM; TEXTURE=1`
5. `QUALITY=HIGH`
6. `QUALITY=HIGH; TEXTURE=1`

**Output Files:**
- `Shader_QUALITY_LOW.efb`
- `Shader_QUALITY_LOW_TEXTURE.efb`
- `Shader_QUALITY_MEDIUM.efb`
- `Shader_QUALITY_MEDIUM_TEXTURE.efb`
- `Shader_QUALITY_HIGH.efb`
- `Shader_QUALITY_HIGH_TEXTURE.efb`

**Use Cases:**
- Quality levels: LOW, MEDIUM, HIGH, ULTRA
- Shadow techniques: NOSHADOW, SIMPLESHADOW, PCFSHADOW
- Anti-aliasing modes: FXAA_OFF, FXAA_LOW, FXAA_HIGH
- Resolution-dependent optimizations

## Grouped Values - Example

For complex shader feature combinations where certain defines must appear together, use grouped syntax:

```xml
<Root>
	<MultiCompile>[LOW,LIGHTNING];[MEDIUM,LIGHTNING,SIMPLESHADOW];[HIGH,LIGHTNING,PCFSHADOW]</MultiCompile>
	<MultiCompile>TEXTURE;_</MultiCompile>
</Root>
```

**Explanation:**
- Square brackets `[]` group multiple defines together
- Commas (`,`) separate defines within a group
- Each group is a complete alternative at that `<MultiCompile>` level
- The underscore `_` can still be used for "no define"

**Generated Variants (6 total):**
1. `LIGHTNING=1; MEDIUM=1; PCFSHADOW=1`
2. `LIGHTNING=1; MEDIUM=1; PCFSHADOW=1; TEXTURE=1`
3. `LIGHTNING=1; SIMPLESHADOW=1`
4. `LIGHTNING=1; SIMPLESHADOW=1; TEXTURE=1`
5. `LIGHTNING=1; LOW=1`
6. `LIGHTNING=1; LOW=1; TEXTURE=1`

Wait, that's not quite right. Let me reconsider the grouped values.

Looking at the XML more carefully:
- `[LOW,LIGHTNING]` - A group containing LOW and LIGHTNING
- `[MEDIUM,LIGHTNING,SIMPLESHADOW]` - A group containing MEDIUM, LIGHTNING, and SIMPLESHADOW
- `[HIGH,LIGHTNING,PCFSHADOW]` - A group containing HIGH, LIGHTNING, and PCFSHADOW

So the first MultiCompile generates 3 variants (one per group), and combined with the second MultiCompile:

**Generated Variants (6 total):**
1. `LOW=1; LIGHTNING=1`
2. `LOW=1; LIGHTNING=1; TEXTURE=1`
3. `MEDIUM=1; LIGHTNING=1; SIMPLESHADOW=1`
4. `MEDIUM=1; LIGHTNING=1; SIMPLESHADOW=1; TEXTURE=1`
5. `HIGH=1; LIGHTNING=1; PCFSHADOW=1`
6. `HIGH=1; LIGHTNING=1; PCFSHADOW=1; TEXTURE=1`

**Output Files:**
- `Shader_HIGH_LIGHTNING_PCFSHADOW.efb`
- `Shader_HIGH_LIGHTNING_PCFSHADOW_TEXTURE.efb`
- `Shader_LIGHTNING_LOW.efb`
- `Shader_LIGHTNING_LOW_TEXTURE.efb`
- `Shader_LIGHTNING_MEDIUM_PCFSHADOW.efb`
- `Shader_LIGHTNING_MEDIUM_PCFSHADOW_TEXTURE.efb`

**Use Cases:**
- Quality tiers with required features: "Low quality skips shadows, Medium adds simple shadows, High uses PCF shadows"
- Platform-specific combinations: "Mobile = no lighting + optimizations, Desktop = full lighting + effects"
- Feature bundles: "Certain post-process effects require specific lighting setups"

**Advanced Example with Mixed Types:**

```xml
<Root>
	<MultiCompile>MOBILE;PC_LOW;[PC_HIGH,ADVANCED_EFFECTS]</MultiCompile>
	<MultiCompile>USE_NORMAL_MAP;_</MultiCompile>
	<MultiCompile>ANTIALIASING=FXAA;ANTIALIASING=SMAA;_</MultiCompile>
</Root>
```

This creates 3 × 2 × 3 = 18 variants combining platform profiles, normal mapping support, and AA techniques.

## References

This project works with effect files and compilation tools from several game development frameworks:

### Frameworks Supported

- **MonoGame** - Cross-platform game framework supporting DirectX 11 and OpenGL
  - Uses `mgfxc` compiler for effect files
  - Profiles: `DirectX_11`, `OpenGL`
  
- **FNA** - XNA Framework re-implementation
  - Uses `fxc` (DirectX FX Compiler)
  - Fixed profile: `fx_2_0`

### Compiler Tools

- **mgfxc** - MonoGame Effect Compiler
  - Compiles `.fx` files to `.efb` (Effect Binary Format)
  - Supports multiple target profiles
  - Integrated with MonoGame content pipeline
  - [MonoGame Documentation](https://docs.monogame.net/)

- **fxc** - Microsoft DirectX Effect Compiler
  - Legacy but still used by FNA
  - Compiles shader effects for Direct3D
  - Part of Windows SDK / DirectX SDK
  - Produces binary effect files

### Effect File Format

- **.fx files** - HLSL effect source files
  - Contains shader code (vertex, pixel, geometry shaders)
  - Defines techniques and passes
  - Supports preprocessor directives (`#define`, `#if`, etc.)

### XML Variant Definition

- **.xml files** - Variant configuration files (created by user)
  - Defines `<MultiCompile>` elements
  - Controls which shader permutations to generate
  - Enables data-driven shader variant management

### Related Tools & Projects

- **Content Pipeline** - MonoGame's asset compilation system
  - efscriptgen output integrates with custom content processor
  - Effect files are typically compiled during game build
  
- **Shader Compilation** - General references
  - [HLSL Preprocessor Directives](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/preprocessor-directives--directx-hlsl-)
  - [Effect Framework (DirectX)](https://docs.microsoft.com/en-us/windows/win32/direct3d11/effects-11)

