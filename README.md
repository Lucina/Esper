# Esper
 
![.NET Core](https://github.com/Lucina/Esper/workflows/.NET%20Core/badge.svg?branch=master)

| Package                | Release |
|------------------------|---------|
| `Esper`           | [![NuGet](https://img.shields.io/nuget/v/Esper.svg)](https://www.nuget.org/packages/Esper/)|
| `Esper.Accelerator`           | [![NuGet](https://img.shields.io/nuget/v/Esper.Accelerator.svg)](https://www.nuget.org/packages/Esper.Accelerator/)|
| `Esper.Misaka`           | [![NuGet](https://img.shields.io/nuget/v/Esper.Misaka.svg)](https://www.nuget.org/packages/Esper.Misaka/)|
| `Esper.Zstandard`           | [![NuGet](https://img.shields.io/nuget/v/Esper.Zstandard.svg)](https://www.nuget.org/packages/Esper.Zstandard/)|

## Synopsis

### Esper

Esper common utilities

[![NuGet](https://img.shields.io/nuget/v/Esper.svg)](https://www.nuget.org/packages/Esper/)

Contains Stream wrappers for other memory types, path filter with fair gitignore support, console hex output with ansi color-coding, etc.

### Esper.Accelerator

Runtime interop utilities

[![NuGet](https://img.shields.io/nuget/v/Esper.Accelerator.svg)](https://www.nuget.org/packages/Esper.Accelerator/)

Currently provides wrappers for platform-specific unmanaged library loading through P/Invoke to system loader libraries.
Supports custom loader classes for other platforms (defaults provided for osx-x64, linux-x64, win-x86, and win-x64).

### Esper.Misaka

Misaka Zstd compressed container library

[![NuGet](https://img.shields.io/nuget/v/Esper.Misaka.svg)](https://www.nuget.org/packages/Esper.Misaka/)

Provides block-compressed archive format using Zstd (and optional Blake2b block hashes for patching etc).
Meant to (eventually) provide fairly efficient patching based on heuristic re-organization and heavy use of block hashes.

### Esper.Zstandard

Zstd wrapper library

[![NuGet](https://img.shields.io/nuget/v/Esper.Zstandard.svg)](https://www.nuget.org/packages/Esper.Zstandard/)

Modified version of [Zstandard.Net](https://github.com/bp74/Zstandard.Net) with general support for any platform with a supported `Esper.Accelerator.IAccelerateLoader` and native build of [zstd](https://github.com/facebook/zstd).
Zstd library binaries provided for osx-x64, linux-x64, win-x86, and win-x64.

Targets zstd library version `1.4.4`.
