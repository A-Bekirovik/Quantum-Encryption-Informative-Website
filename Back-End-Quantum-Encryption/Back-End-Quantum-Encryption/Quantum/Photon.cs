namespace Back_End_Quantum_Encryption.Quantum;

/// <summary>
/// A single polarized photon: the qubit that travels from Alice to Bob.
///
/// The basis and bit are private on purpose. Whoever receives the photon cannot
/// "look inside" it; the only thing they can do is <see cref="Measure"/> it,
/// exactly like in real quantum mechanics.
/// </summary>
public sealed class Photon
{
    private Basis _basis;
    private int _bit;

    private Photon(Basis basis, int bit)
    {
        _basis = basis;
        _bit = bit;
    }

    /// <summary>Prepares a photon that encodes <paramref name="bit"/> in <paramref name="basis"/>.</summary>
    public static Photon Prepare(int bit, Basis basis)
    {
        if (bit is not (0 or 1))
            throw new ArgumentOutOfRangeException(nameof(bit), "A bit must be 0 or 1.");
        return new Photon(basis, bit);
    }

    /// <summary>The polarization angle in degrees (0, 45, 90 or 135). Only used for visualisation.</summary>
    public int PolarizationDegrees => PolarizationOf(_bit, _basis);

    /// <summary>
    /// Measures the photon with a polarization filter in <paramref name="measurementBasis"/>.
    /// Same basis as it was prepared in: the result is the encoded bit, always.
    /// Other basis: the result is 50/50 random, and the photon collapses into the
    /// measured state, so the original information is destroyed. That collapse is
    /// what makes an eavesdropper detectable.
    /// </summary>
    public int Measure(Basis measurementBasis, Random rng)
    {
        if (measurementBasis != _basis)
        {
            _bit = rng.Next(2);
            _basis = measurementBasis;
        }
        return _bit;
    }

    public static int PolarizationOf(int bit, Basis basis) => basis switch
    {
        Basis.Rectilinear => bit == 0 ? 0 : 90,
        Basis.Diagonal => bit == 0 ? 45 : 135,
        _ => throw new ArgumentOutOfRangeException(nameof(basis))
    };
}
