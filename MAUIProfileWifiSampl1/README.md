# MAUIProfileWifiSampl1

A .NET MAUI port of the Zebra **EMDK-Xamarin ProfileWifiSample1**. It uses the
EMDK `ProfileManager` to apply a **Wi-Fi** profile on a Zebra Android device:
enable/disable the Wi-Fi radio and optionally **Add / Remove / Connect /
Disconnect** a network by SSID.

## How it maps to the original Xamarin sample

The original sample was a single `MainActivity`. Because EMDK is Android-only,
the MAUI version splits the logic along the standard cross-platform seam:

| Concern | Original (Xamarin) | This project (MAUI) |
| --- | --- | --- |
| UI | `Resources/layout/*.axml` | `MainPage.xaml` (cross-platform) |
| EMDK init + apply | `MainActivity.cs` | `Platforms/Android/Services/WifiProfileService.cs` |
| Contract | (none) | `Services/IWifiProfileService.cs` |
| Off-device build | (none) | `Services/StubWifiProfileService.cs` |
| Profile config | `Assets/EMDKConfig.xml` | `Platforms/Android/Assets/EMDKConfig.xml` |
| Permissions | `Properties/AndroidManifest.xml` | `Platforms/Android/AndroidManifest.xml` |

### Callback → async

EMDK is callback-based:

- `EMDKManager.GetEMDKManager` → result arrives in `IEMDKListener.OnOpened/OnClosed`.
- `ProfileManager.ProcessProfileAsync` → result arrives in the `ProfileManager.Data` event.

`WifiProfileService` wraps both with `TaskCompletionSource`, so the shared UI can
simply `await InitializeAsync()` and `await ApplyWifiProfileAsync(...)`.

The Wi-Fi provisioning XML built in `BuildProfileXml` and the `CheckXmlError`
result parsing are faithful ports of `SetProfile()` / `CheckXmlError()` from the
original `MainActivity`.

### Dependency injection

`MauiProgram.cs` registers the platform implementation:

```csharp
#if ANDROID
    builder.Services.AddSingleton<IWifiProfileService, Platforms.Android.Services.WifiProfileService>();
#else
    builder.Services.AddSingleton<IWifiProfileService, StubWifiProfileService>();
#endif
builder.Services.AddTransient<MainPage>();
```

`MainPage` receives `IWifiProfileService` via constructor injection; Shell
resolves the page from the DI container.

## EMDK binding note

`Libs/Symbol.XamarinEMDK.dll` (v4.0.0.3) is the legacy Xamarin.Android binding,
copied from the original sample's NuGet package and referenced for the Android
target only. It compiles and links against `net10.0-android` in this project.

If you hit runtime `TypeLoadException`/binding issues on newer EMDK versions,
rebuild the binding for .NET from the official EMDK `.aar` using an
**Android Binding Library** project, then swap the `<Reference>` in the
`.csproj`. The C# usage in `WifiProfileService` stays the same.

## Build & run

```powershell
dotnet build -f net10.0-android -c Debug
# deploy to a connected Zebra device:
dotnet build -f net10.0-android -t:Run
```

Run on a **Zebra** device with EMDK runtime installed. On Windows/iOS/MacCatalyst
the app runs against `StubWifiProfileService` for UI/layout work only.
