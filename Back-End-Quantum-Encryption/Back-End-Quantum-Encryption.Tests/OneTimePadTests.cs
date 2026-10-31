using Back_End_Quantum_Encryption.Crypto;

namespace Back_End_Quantum_Encryption.Tests;

public class OneTimePadTests
{
    [Fact]
    public void EncryptThenDecrypt_ReturnsOriginalMessage()
    {
        string key = string.Concat(Enumerable.Range(0, 200).Select(i => (i * 7 % 3 == 0) ? '1' : '0'));
        var encrypted = OneTimePad.Encrypt("Hallo Bob! ⚛", key);

        Assert.Equal("Hallo Bob! ⚛", OneTimePad.Decrypt(encrypted.CiphertextHex, key));
        Assert.False(encrypted.KeyReused);
    }

    [Fact]
    public void Ciphertext_IsPlaintextXorKey()
    {
        // 'A' = 01000001
        var encrypted = OneTimePad.Encrypt("A", "11110000");

        Assert.Equal("01000001", encrypted.PlaintextBits);
        Assert.Equal("10110001", encrypted.CiphertextBits);
        Assert.Equal("B1", encrypted.CiphertextHex);
    }

    [Fact]
    public void WrongKey_DoesNotRecoverMessage()
    {
        var encrypted = OneTimePad.Encrypt("secret", "1010101010101010101010101010101010101010101010101");
        string wrong = OneTimePad.Decrypt(encrypted.CiphertextHex, "0110101010101010101010101010101010101010101010101");

        Assert.NotEqual("secret", wrong);
    }

    [Fact]
    public void ShortKey_IsRepeatedAndFlagged()
    {
        var encrypted = OneTimePad.Encrypt("AB", "1010");

        Assert.True(encrypted.KeyReused);
        Assert.Equal("1010101010101010", encrypted.KeyBitsUsed);
        Assert.Equal("AB", OneTimePad.Decrypt(encrypted.CiphertextHex, "1010"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("10201")]
    public void InvalidKey_Throws(string key)
    {
        Assert.Throws<ArgumentException>(() => OneTimePad.Encrypt("hi", key));
    }
}
