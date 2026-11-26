# Third-party notices

**Maritime Trainer (MaritimeTrainer)** is © 2026 Per Hejndorf and is licensed under the [MIT License](LICENSE), except where stated below. This file lists the third-party components and assets it uses, each under its own licence. Licence texts and copyright notices are kept by the upstream projects at the *Source* links; where a licence requires its notice to travel with binaries, the upstream package already carries it.

_Last reviewed: 2026-11-26. Versions are the ones resolved in this repository at that date; Dependabot keeps them current, so check the project files for the exact version of a given release._

## Libraries included in the application

| Library | Version | Licence | Source | Modified |
|---|---|---|---|---|
| .NET runtime and Blazor WebAssembly (shipped in `_framework/`) | 10.0 | MIT | [dotnet/runtime](https://github.com/dotnet/runtime), [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore) | No |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly/10.0.15) | No |

<details>
<summary>Indirect (transitive) dependencies — 25 packages</summary>

These packages are pulled in by the libraries above and are shipped with the application. All are used unmodified.

| Library | Version | Licence | Source | Modified |
|---|---|---|---|---|
| Microsoft.AspNetCore.Authorization | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Authorization/10.0.15) | No |
| Microsoft.AspNetCore.Components | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Components/10.0.15) | No |
| Microsoft.AspNetCore.Components.Forms | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Forms/10.0.15) | No |
| Microsoft.AspNetCore.Components.Web | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.Web/10.0.15) | No |
| Microsoft.AspNetCore.Metadata | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Metadata/10.0.15) | No |
| Microsoft.Extensions.Configuration | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/10.0.15) | No |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/10.0.15) | No |
| Microsoft.Extensions.Configuration.Binder | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder/10.0.15) | No |
| Microsoft.Extensions.Configuration.FileExtensions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.FileExtensions/10.0.15) | No |
| Microsoft.Extensions.Configuration.Json | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/10.0.15) | No |
| Microsoft.Extensions.DependencyInjection | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.15) | No |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/10.0.15) | No |
| Microsoft.Extensions.Diagnostics | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics/10.0.15) | No |
| Microsoft.Extensions.Diagnostics.Abstractions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.Abstractions/10.0.15) | No |
| Microsoft.Extensions.FileProviders.Abstractions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Abstractions/10.0.15) | No |
| Microsoft.Extensions.FileProviders.Physical | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Physical/10.0.15) | No |
| Microsoft.Extensions.FileSystemGlobbing | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing/10.0.15) | No |
| Microsoft.Extensions.Logging | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Logging/10.0.15) | No |
| Microsoft.Extensions.Logging.Abstractions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.15) | No |
| Microsoft.Extensions.Options | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Options/10.0.15) | No |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/10.0.15) | No |
| Microsoft.Extensions.Primitives | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Primitives/10.0.15) | No |
| Microsoft.Extensions.Validation | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Validation/10.0.15) | No |
| Microsoft.JSInterop | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.JSInterop/10.0.15) | No |
| Microsoft.JSInterop.WebAssembly | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.JSInterop.WebAssembly/10.0.15) | No |

</details>

## Build, test and tooling (not distributed)

Used to build, test or render promotional material only; not part of the shipped application.

| Library | Version | Licence | Source | Modified |
|---|---|---|---|---|
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 10.0.15 | MIT | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly.DevServer/10.0.15) | No |

## Icons, images, audio and video

| Asset(s) | Origin | Licence |
|---|---|---|
| `art/icon.svg`, `src/MaritimeTrainer/wwwroot/icons/` (8 files: .png, rendered from `art/icon.svg` by `tools/make-icons.ps1`) | Drawn by the author as SVG (written with an AI coding agent), not made with an image-generation tool. The anchor is the ⚓ emoji in Microsoft's Fluent Emoji design, rendered from the Segoe UI Emoji font | MIT (as part of this project); the anchor emoji design is © Microsoft, MIT ([microsoft/fluentui-emoji](https://github.com/microsoft/fluentui-emoji)) |
| `images/banner.png` | Screenshot of the app, taken by `tools/make-banner.ps1` | MIT (as part of this project) |
| Signal flags, pennants, buoys, topmarks, lamps, time bands and sector diagrams in the app | Drawn as SVG/CSS by the app's own code (`Models/FlagArt.cs`, `Models/BuoyArt.cs`, `Components/`) | MIT (as part of this project) |
| Morse tones and answer sounds | Generated at run time with the browser's Web Audio API; no audio files are shipped | — |
| Emoji in the interface (⚓ 🛟 💡 🧭 ✍️ and others) | Not shipped: drawn at run time by the fonts installed on the user's device | — |

The flag, buoy and light designs follow the public standards they teach (the International Code of Signals and the IALA Maritime Buoyage System); the drawings themselves are the project's own.

## Trademarks

IALA is the International Organization for Marine Aids to Navigation; its name is used only to identify the buoyage system the app teaches. Microsoft, Windows, .NET, Blazor and other Microsoft product names are trademarks of the Microsoft group of companies. All other product names, logos and brands mentioned in this repository are the property of their respective owners. They are used only to identify the services and tools this project works with, and their use does not imply endorsement.
