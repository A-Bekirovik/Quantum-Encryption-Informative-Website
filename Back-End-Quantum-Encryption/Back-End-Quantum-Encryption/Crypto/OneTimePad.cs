using System.Text;

namespace Back_End_Quantum_Encryption.Crypto;

/// <summary>The outcome of encrypting a message with a BB84 key.</summary>
public sealed record EncryptionResult(
    string Plaintext,
    string PlaintextBits,
    string KeyBitsUsed,
    string CiphertextBits,
    string CiphertextHex,
    bool KeyReused);

/// <summary>
/// One-time pad: ciphertext = plaintext XOR key, bit by bit. Decrypting is the same
/// XOR again, because (m XOR k) XOR k = m.
///
/// It is only perfectly secure if the key is at least as long as the message and is
/// used once. If the BB84 key is shorter, it is repeated and <see cref="EncryptionResult.KeyReused"/>
/// is set, so the front-end can warn that this is no longer a true one-time pad.
/// </summary>
public static class OneTimePad
{
    public static EncryptionResult Encrypt(string message, string keyBits)
    {
        ValidateKey(keyBits);
        byte[] plain = Encoding.UTF8.GetBytes(message);
        byte[] cipher = Xor(plain, keyBits);
        int bitCount = plain.Length * 8;

        return new EncryptionResult(
            Plaintext: message,
            PlaintextBits: ToBits(plain),
            KeyBitsUsed: RepeatKey(keyBits, bitCount),
            CiphertextBits: ToBits(cipher),
            CiphertextHex: Convert.ToHexString(cipher),
            KeyReused: keyBits.Length < bitCount);
    }

    public static string Decrypt(string ciphertextHex, string keyBits)
    {
        ValidateKey(keyBits);
        byte[] cipher = Convert.FromHexString(ciphertextHex);
        return Encoding.UTF8.GetString(Xor(cipher, keyBits));
    }

    private static byte[] Xor(byte[] data, string keyBits)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            byte keyByte = 0;
            for (int b = 0; b < 8; b++)
            {
                char k = keyBits[(i * 8 + b) % keyBits.Length];
                keyByte = (byte)((keyByte << 1) | (k == '1' ? 1 : 0));
            }
            result[i] = (byte)(data[i] ^ keyByte);
        }
        return result;
    }

    private static string RepeatKey(string keyBits, int length)
    {
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++) sb.Append(keyBits[i % keyBits.Length]);
        return sb.ToString();
    }

    private static string ToBits(byte[] bytes) =>
        string.Concat(bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

    private static void ValidateKey(string keyBits)
    {
        if (string.IsNullOrEmpty(keyBits))
            throw new ArgumentException("The key is empty.", nameof(keyBits));
        if (keyBits.Any(c => c is not ('0' or '1')))
            throw new ArgumentException("The key may only contain 0 and 1.", nameof(keyBits));
    }
}
