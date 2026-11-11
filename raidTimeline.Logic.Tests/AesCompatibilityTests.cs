using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace raidTimeline.Logic.Tests;

/// <summary>
/// The endpoint upload used to encrypt with RijndaelManaged (defaults: 128 bit block, CBC, PKCS7).
/// This guards that the switch to AES yields identical ciphertext for the receiving endpoint.
/// </summary>
public class AesCompatibilityTests
{
    [Fact]
    public void Aes_ProducesSameCiphertextAsLegacyRijndael()
    {
        var key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var iv = Enumerable.Range(100, 16).Select(i => (byte)i).ToArray();
        var data = Enumerable.Range(0, 1000).Select(i => (byte)(i * 7)).ToArray();

#pragma warning disable SYSLIB0022
        using var legacy = new RijndaelManaged();
        var expected = legacy.CreateEncryptor(key, iv).TransformFinalBlock(data, 0, data.Length);
#pragma warning restore SYSLIB0022

        using var aes = Aes.Create();
        var actual = aes.CreateEncryptor(key, iv).TransformFinalBlock(data, 0, data.Length);

        Assert.Equal(expected, actual);
    }
}
