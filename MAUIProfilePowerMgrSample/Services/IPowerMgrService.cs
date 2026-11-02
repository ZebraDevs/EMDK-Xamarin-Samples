namespace MAUIProfilePowerMgrSample1.Services;

/// <summary>
/// Cross-platform contract for the Zebra EMDK Power Manager profile feature.
/// The shared UI talks to this interface; the concrete implementation lives in
/// Platforms/Android and uses the EMDK ProfileManager.
/// </summary>
public interface IPowerMgrService
{
    /// <summary>Raised whenever the EMDK/profile status changes (status text).</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Creates the EMDKManager and obtains the ProfileManager.</summary>
    void Initialize();

    /// <summary>
    /// Applies the PowerMgr profile. <paramref name="suspend"/> = true sets the
    /// ResetAction to Suspend (sleep); false sets it to Reset (reboot).
    /// </summary>
    void SetProfile(bool suspend);

    /// <summary>Releases EMDK resources.</summary>
    void Release();
}
