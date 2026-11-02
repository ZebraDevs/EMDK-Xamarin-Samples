namespace MAUIMultiBarcodeSample1.Services;

/// <summary>A single decoded barcode: its symbology (Type) and decoded Value.</summary>
public record ScannedBarcode(string Type, string Value);
