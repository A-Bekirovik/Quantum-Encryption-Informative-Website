using Back_End_Quantum_Encryption.Crypto;
using Back_End_Quantum_Encryption.Quantum;

namespace Back_End_Quantum_Encryption.Models;

public sealed record SimulateRequest(
    int QubitCount = 32,
    bool Eavesdropper = false,
    double SampleFraction = 0.25,
    double QberThreshold = 0.11,
    int? Seed = null);

public sealed record EncryptRequest(
    string Message,
    bool Eavesdropper = false,
    double SampleFraction = 0.25,
    double QberThreshold = 0.11,
    int? Seed = null);

/// <param name="Simulation">The BB84 run that produced the key.</param>
/// <param name="Encryption">Alice's encryption. Null when the key was rejected.</param>
/// <param name="BobDecrypted">What Bob reads when he decrypts with his own key. Null when the key was rejected.</param>
/// <param name="Summary">One sentence describing the outcome.</param>
public sealed record EncryptResponse(
    Bb84Result Simulation,
    EncryptionResult? Encryption,
    string? BobDecrypted,
    string Summary);

public sealed record DecryptRequest(string CiphertextHex, string Key);

public sealed record DecryptResponse(string Plaintext);
