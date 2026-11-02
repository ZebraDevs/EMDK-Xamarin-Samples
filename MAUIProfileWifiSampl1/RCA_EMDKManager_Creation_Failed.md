# RCA – "EMDKManager object creation failed ..." (MAUIProfileWifiSampl1)

| Field | Value |
| --- | --- |
| Application | MAUIProfileWifiSampl1 (`com.companyname.mauiprofilewifisampl1`), .NET MAUI `net10.0-android` |
| Target SDK | **36** (the .NET 10 MAUI default; confirmed in logcat and in the merged manifest) |
| EMDK binding | NuGet `Symbol.XamarinEMDK` 8.0.0.2 |
| Customer device | Zebra **TC53**, **Android 13**, BSP `13-33-18.00-TG-U00-STD-ATH-04`, S/N 25094524212339 |
| Log source | RxLogger 13.0.12.72 bundle "RxLogger 30", captured 2026-10-15 |
| Reported symptom | Status label shows **"EMDKManager object creation failed ..."**; Wi-Fi profile cannot be applied |
| Severity | High: no EMDK means no profile can be applied |
| **Root cause** | The app targets API 36 but its manifest has no `<queries>` entry for `com.symbol.emdk.emdkservice`. Android 11+ package visibility hides the EMDK service from the app, so the bind fails. |
| **Fix** | Add `<queries><package android:name="com.symbol.emdk.emdkservice" /></queries>` to `AndroidManifest.xml` and ship a new APK |

---

## 1. Customer report

- The customer installed the MAUI Wi-Fi profile app on their Zebra device and launched it.
- The app immediately showed **"EMDKManager object creation failed ..."** instead of "EMDK open success.".
- As a result, Apply did not configure Wi-Fi.
- The same failure is visible in the RxLogger logs attached to the support ticket.

## 2. Where the message comes from (code)

`Platforms/Android/Services/WifiProfileService.cs`, `InitializeAsync()` (called from `MainPage.OnAppearing`):

```csharp
EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);
if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
    _initTcs.TrySetResult("EMDKManager object creation failed ...");
```

`GetEMDKManager()` binds to the Zebra EMDK service `com.symbol.emdk.emdkservice/.EMDKService`
(intent action `symbol.intent.action.emdkservice`). If that bind fails, `GetEMDKManager()` returns a
non-`SUCCESS` status and the app shows this message. The original code dropped the status code, so the
screen gave no hint of the cause.

## 3. Log analysis

### 3.1 Timeline (2026-10-15, device time)

| Time | Log (file:line) | Event |
| --- | --- | --- |
| 16:38:35 | `Event0.txt:533` | Boot: system grants runtime permissions to **`com.symbol.emdk.emdkservice`**, so the EMDK service package is installed |
| 16:42:11 | `System0.txt:7598` | App installed: `packages added: com.companyname.mauiprofilewifisampl1 uid: 10249` |
| 16:42:48 | `Main0.txt:150` | DataWedge running normally (`DataWedge-ScannerProxyHandler ... Open Scanner [Finish]`) |
| 16:42:59.251 | `System0.txt:8674` | App cold start: `Start proc 14715:com.companyname.mauiprofilewifisampl1/u0a249` (user 0) |
| 16:42:59.306 | `Main0.txt:4370` | EMDK library loaded into the app: `/system/framework/com.symbol.emdk.jar ... target_sdk_version=36` |
| **16:42:59.868** | **`System0.txt:8713`** | **`W ActivityManager: Unable to start service Intent { act=symbol.intent.action.emdkservice cmp=com.symbol.emdk.emdkservice/.EMDKService } U=0: not found`** |
| 16:43:00.170 | `Main0.txt:5085` | MainActivity displayed; the UI shows the "EMDKManager object creation failed ..." status |
| 16:43:33 – 16:44:06 | `Event0.txt:5457+` | App resumed from the launcher and again after screen sleep. EMDK is never connected (no `EMDK_Service_Connection` lines anywhere in the bundle) |

### 3.2 Interpretation

- The failing call is the **bind from the app to the EMDK service**, 0.6 s after the process started.
  This matches exactly the point where `InitializeAsync()` runs.
- ActivityManager reports the service as **"not found"**, but the bundle shows the package **is installed**
  (permissions granted at boot, and the Play Store enumerates `com.symbol.emdk.emdkservice` at
  `Main0.txt` 16:42:51). The EMDK framework jar is also present and loaded.
- An installed service that ActivityManager reports as "not found" to one particular caller is the
  signature of **Android 11+ package visibility filtering**. Since API 30, an app cannot resolve or bind to
  another app's components unless that package is declared in `<queries>` in its manifest (or the app
  holds `QUERY_ALL_PACKAGES`).
- The app targets **API 36** (`target_sdk_version=36`, `Main0.txt:4370`). Its manifest
  (`Platforms/Android/AndroidManifest.xml`) had **no `<queries>` element**, so the EMDK service was
  hidden from it.

### 3.3 Hypotheses ruled out by the logs

| Hypothesis | Verdict | Evidence |
| --- | --- | --- |
| EMDK runtime/library missing on the device | Ruled out | `com.symbol.emdk.jar` loaded into the app (`Main0.txt:4370`) |
| EMDK service app not installed | Ruled out | Runtime permissions granted to `com.symbol.emdk.emdkservice` at boot (`Event0.txt:533-537`, `1645-1646`) |
| DataWedge disabled | Ruled out | DataWedge active throughout the capture (`DataWedge-ScannerProxyHandler` lines). On Android 13 the EMDK service is the standalone `com.symbol.emdk.emdkservice` package, not DataWedge |
| Secondary user / work profile | Ruled out | `U=0`, process uid `u0a249` |
| Launched before EMDK was ready after boot | Ruled out | Device booted about 4.5 min before launch, and the failure repeats on later resumes |
| App crash / native fault | Ruled out | `Crash0.txt` contains only the header; no ANR or tombstone in the bundle |
| Missing EMDK permission | Ruled out | `com.symbol.emdk.permission.EMDK` is declared. The failure is at service resolution ("not found"), not a permission denial |

## 4. Root cause

**Primary (direct) cause:** the Android manifest of the MAUI app doesn't declare package visibility for the
EMDK service. With `targetSdkVersion 36`, Android 13 filters `com.symbol.emdk.emdkservice` out of the
app's view. `EMDKManager.GetEMDKManager()` therefore cannot bind to `.EMDKService`, and the app reports
"EMDKManager object creation failed ...".

**Why it was introduced (5 whys):**
1. Why did EMDK fail to open? The bind to `com.symbol.emdk.emdkservice/.EMDKService` returned "not found".
2. Why "not found" when the package is installed? Android 11+ package visibility hid it from the app.
3. Why was it hidden? The app targets API 36 and its manifest has no `<queries>` entry for the service.
4. Why is the entry missing? The manifest was ported from the Xamarin ProfileWifiSample1, which targeted a
   pre-API 30 SDK where package visibility didn't exist. The .NET 10 MAUI template silently raised the
   target SDK to 36.
5. Why wasn't it caught? The app showed a generic message without the EMDK status code, and the port
   wasn't validated on an Android 11+ Zebra device before release to the customer.

**Contributing cause:** `InitializeAsync()` discarded `StatusCode` / `ExtendedStatusCode`, so neither the
customer nor support could see the cause on screen or in the app's own logs.

## 5. Reproduction

Expected to reproduce on **any Zebra device on Android 11 or later** with the unfixed build (no device was
attached during this analysis, so it has not yet been re-run in-house):

1. Install the build **without** `<queries>` and launch it. Expect `EMDKManager object creation failed ...`
   and logcat `Unable to start service ... com.symbol.emdk.emdkservice/.EMDKService } U=0: not found`.
2. Install the **fixed** build. Expect `EMDK open success.` and logcat
   `EMDK_Service_Connection: #EMDK-API# The EMDK Service is now connected!`.
3. Optional check: `adb shell dumpsys package queries`. With the fix, the app's entry should list
   `com.symbol.emdk.emdkservice` as visible.

## 6. Resolution

### 6.1 Code changes (applied in this repo, `dotnet build -f net10.0-android` succeeds with 0 warnings)

1. `Platforms/Android/AndroidManifest.xml`: declare the EMDK service package. Verified present in the
   merged `obj/Debug/net10.0-android/android/AndroidManifest.xml`.
   ```xml
   <queries>
       <package android:name="com.symbol.emdk.emdkservice" />
   </queries>
   ```
2. `Platforms/Android/Services/WifiProfileService.cs`: the failure message now includes
   `status=<StatusCode>, ext=<ExtendedStatusCode>`, and the failure is written to logcat with the tag
   `WifiProfileService`.

### 6.2 Customer action

- No device-side workaround: the visibility rule is enforced by Android based on the APK's manifest.
- Install the updated APK (new build with the manifest fix). No device configuration changes are needed.
- If the issue persists with the new build, send a fresh RxLogger bundle captured from app launch, plus
  the new on-screen status text (it now includes the EMDK status codes).

### 6.3 Verification plan

- On a TC53 running Android 13 (customer's BSP, or close to it), launch the fixed build. Expect `EMDK open success.`
- Apply each Wi-Fi action (Add / Remove / Connect / Disconnect, and enable/disable Wi-Fi). Expect the
  profile to apply successfully.
- Run a regression check on one Android 11 and one Android 14/15 Zebra device.

## 7. Recommended follow-ups (not the cause of this ticket)

| Location | Problem | Recommendation |
| --- | --- | --- |
| `WifiProfileService.OnClosed()` | Completes an already-completed TCS, so the UI isn't told when EMDK disconnects | Raise a status event and re-initialize |
| `MainPage.OnAppearing` | Init only runs on page appear, not on app resume | Re-init on resume when the profile manager is null |
| `InitializeAsync` | Single attempt, no retry | Short retry with backoff |
| `README.md` | Says the binding is `Libs/Symbol.XamarinEMDK.dll` v4.0.0.3, but the project uses NuGet 8.0.0.2 | Correct the documentation |

## 8. Preventive actions

- Add an "Android 11+ package visibility" item to the checklist for porting Xamarin EMDK samples to MAUI:
  EMDK apps targeting API 30+ need `<queries>` for `com.symbol.emdk.emdkservice`.
- Validate ports on at least one Zebra Android 11+ device before delivering them to a customer.
- Always surface EMDK status codes in error messages and logcat.
