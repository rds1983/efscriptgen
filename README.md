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
	<MultiCompile>LIGHTS=4;LIGHTS=8;LIGHTS=16</MultiCompile>
	<MultiCompile>TEXTURE;_</MultiCompile>
</Root>
```

**Explanation:**
- `LIGHTS=4` - Defines `LIGHTS` macro with numeric value `4`
- `LIGHTS=8` - Defines `LIGHTS` macro with numeric value `8`
- `LIGHTS=16` - Defines `LIGHTS` macro with numeric value `16`
- Multiple key-value pairs separated by semicolons create alternatives
- The second `<MultiCompile>` still uses Boolean variants

**Generated Variants (6 total):**
1. `LIGHTS=4`
2. `LIGHTS=4; TEXTURE=1`
3. `LIGHTS=8`
4. `LIGHTS=8; TEXTURE=1`
5. `LIGHTS=16`
6. `LIGHTS=16; TEXTURE=1`

**Output Files:**
- `Shader_LIGHTS_4.efb`
- `Shader_LIGHTS_4_TEXTURE.efb`
- `Shader_LIGHTS_8.efb`
- `Shader_LIGHTS_8_TEXTURE.efb`
- `Shader_LIGHTS_16.efb`
- `Shader_LIGHTS_16_TEXTURE.efb`

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

## Sponsor
If this project is useful for you, you can support development:
- Boosty: https://boosty.to/rds1983
- Telegram Wallet: https://t.me/rds1983

### Crypto

USDT (TON): `UQCQy6tFInPvqinE44zHY4R0rYS3niaBikkqiSyGmyoAMwyO`

TON: `UQCQy6tFInPvqinE44zHY4R0rYS3niaBikkqiSyGmyoAMwyO`