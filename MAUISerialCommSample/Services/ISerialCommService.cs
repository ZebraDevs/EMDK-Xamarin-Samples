namespace MAUISerialCommSample.Services;

/// <summary>
/// Cross-platform contract for the Zebra EMDK Serial Communication feature.
/// The Android EMDK implementation lives in Platforms/Android.
/// </summary>
public interface ISerialCommService
{
    /// <summary>Raised with a human-readable status / result message.</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Raised when the serial channel becomes usable (true) or not (false).</summary>
    event EventHandler<bool>? ReadyStateChanged;

    /// <summary>Creates the EMDKManager and enables the serial communication channel.</summary>
    void Initialize();

    /// <summary>Writes the given text to the serial channel.</summary>
    void Write(string data);

    /// <summary>Reads from the serial channel (10s timeout).</summary>
    void Read();

    /// <summary>Releases EMDK resources.</summary>
    void Release();
}
