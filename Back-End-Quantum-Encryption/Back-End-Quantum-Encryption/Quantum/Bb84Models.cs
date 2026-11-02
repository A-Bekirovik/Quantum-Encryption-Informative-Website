namespace Back_End_Quantum_Encryption.Quantum;

/// <summary>Settings for one run of the BB84 protocol.</summary>
/// <param name="QubitCount">How many photons Alice sends.</param>
/// <param name="EavesdropperEnabled">If true, Eve intercepts every photon (intercept-resend attack).</param>
/// <param name="SampleFraction">Share of the sifted bits Alice and Bob sacrifice to estimate the error rate.</param>
/// <param name="QberThreshold">Maximum error rate they accept. Above this they assume an eavesdropper and abort.</param>
/// <param name="Seed">Optional seed so a run can be repeated exactly (handy for tests and demos).</param>
public sealed record Bb84Options(
    int QubitCount = 32,
    bool EavesdropperEnabled = false,
    double SampleFraction = 0.25,
    double QberThreshold = 0.11,
    int? Seed = null)
{
    public const int MaxQubits = 20_000;
}

/// <summary>Everything that happens to one photon between Alice and Bob, before sifting.</summary>
public sealed record PhotonRound(
    int Index,
    int AliceBit,
    Basis AliceBasis,
    int SentPolarization,
    Basis? EveBasis,
    int? EveBit,
    int? EveResentPolarization,
    Basis BobBasis,
    int BobBit);

/// <summary>Everything that happened to one photon, from Alice to Bob.</summary>
public sealed record QubitTrace(
    int Index,
    int AliceBit,
    Basis AliceBasis,
    int SentPolarization,
    Basis? EveBasis,
    int? EveBit,
    int? EveResentPolarization,
    Basis BobBasis,
    int BobBit,
    bool BasesMatch,
    bool UsedForErrorCheck,
    bool IsError,
    bool InFinalKey);

/// <summary>A human readable description of one protocol step, for the front-end.</summary>
public sealed record SimulationStep(int Number, string Title, string Description);

/// <summary>The full outcome of a BB84 run.</summary>
public sealed record Bb84Result(
    int QubitCount,
    bool EavesdropperEnabled,
    IReadOnlyList<SimulationStep> Steps,
    IReadOnlyList<QubitTrace> Qubits,
    IReadOnlyList<int> SiftedIndices,
    IReadOnlyList<int> SampleIndices,
    int SampleErrors,
    double Qber,
    double QberThreshold,
    bool EavesdropperDetected,
    bool KeyAccepted,
    string AliceKey,
    string BobKey,
    bool KeysMatch);
