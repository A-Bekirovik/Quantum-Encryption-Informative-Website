namespace Back_End_Quantum_Encryption.Quantum;

/// <summary>
/// The two polarization bases used in BB84.
/// Rectilinear (+): 0 = horizontal (0°), 1 = vertical (90°).
/// Diagonal (×):    0 = 45°,              1 = 135°.
/// </summary>
public enum Basis
{
    Rectilinear,
    Diagonal
}
