using System.Text;
using Back_End_Quantum_Encryption.Crypto;
using Back_End_Quantum_Encryption.Models;
using Back_End_Quantum_Encryption.Quantum;
using Microsoft.AspNetCore.Mvc;

namespace Back_End_Quantum_Encryption.Controllers;

[ApiController]
[Route("api/bb84")]
public sealed class Bb84Controller : ControllerBase
{
    public const int MaxMessageBytes = 500;

    private readonly Bb84Simulator _simulator;

    public Bb84Controller(Bb84Simulator simulator) => _simulator = simulator;

    /// <summary>Runs BB84 once and returns every step and every photon.</summary>
    [HttpPost("simulate")]
    public ActionResult<Bb84Result> Simulate(SimulateRequest request)
    {
        var options = new Bb84Options(request.QubitCount, request.Eavesdropper,
            request.SampleFraction, request.QberThreshold, request.Seed);
        return Run(options, result => result);
    }

    /// <summary>
    /// Runs BB84 with enough photons for the message, then encrypts the message with
    /// Alice's key and lets Bob decrypt it with his key.
    /// </summary>
    [HttpPost("encrypt")]
    public ActionResult<EncryptResponse> Encrypt(EncryptRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
            return Problem("Message is required.", statusCode: 400);
        int messageBits = Encoding.UTF8.GetByteCount(request.Message) * 8;
        if (messageBits > MaxMessageBytes * 8)
            return Problem($"Message is too long (max {MaxMessageBytes} bytes).", statusCode: 400);

        var options = new Bb84Options(QubitsNeeded(messageBits, request.SampleFraction), request.Eavesdropper,
            request.SampleFraction, request.QberThreshold, request.Seed);

        return Run(options, result =>
        {
            if (!result.KeyAccepted)
                return new EncryptResponse(result, null, null,
                    "The key was rejected, so Alice does not send the message.");

            var encryption = OneTimePad.Encrypt(request.Message, result.AliceKey);
            string bobDecrypted = OneTimePad.Decrypt(encryption.CiphertextHex, result.BobKey);
            string summary = bobDecrypted == request.Message
                ? "Bob decrypted the message correctly with his copy of the key."
                : "Bob's key differs from Alice's, so his decryption is garbled.";
            return new EncryptResponse(result, encryption, bobDecrypted, summary);
        });
    }

    /// <summary>Decrypts a hex ciphertext with a key made of 0s and 1s.</summary>
    [HttpPost("decrypt")]
    public ActionResult<DecryptResponse> Decrypt(DecryptRequest request)
    {
        try
        {
            return new DecryptResponse(OneTimePad.Decrypt(request.CiphertextHex, request.Key));
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            return Problem(e.Message, statusCode: 400);
        }
    }

    /// <summary>
    /// About half the photons survive sifting, and the error check takes another share,
    /// so Alice sends roughly 2 / (1 - sampleFraction) photons per key bit, plus a margin.
    /// </summary>
    public static int QubitsNeeded(int keyBits, double sampleFraction)
    {
        double keepRate = 0.5 * (1 - Math.Clamp(sampleFraction, 0, 0.99));
        int needed = (int)Math.Ceiling(keyBits / keepRate * 1.2) + 16;
        return Math.Min(needed, Bb84Options.MaxQubits);
    }

    private ActionResult<T> Run<T>(Bb84Options options, Func<Bb84Result, T> map)
    {
        try
        {
            return map(_simulator.Run(options));
        }
        catch (ArgumentOutOfRangeException e)
        {
            return Problem(e.Message, statusCode: 400);
        }
    }
}
