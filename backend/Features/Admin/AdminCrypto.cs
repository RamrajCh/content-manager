using System.Security.Cryptography;
using System.Text;
using ContentManager.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace ContentManager.Api.Features.Admin;

public sealed class AdminCrypto(IOptions<PlatformOptions> options)
{
    private readonly byte[] encryptionKey = Convert.FromBase64String(options.Value.Security.EncryptionKey);
    private readonly byte[] lookupKey = Convert.FromBase64String(options.Value.Security.LookupKey);
    private readonly string keyVersion = options.Value.Security.KeyVersion;

    public static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string Hash(string purpose, string value) =>
        Convert.ToBase64String(HMACSHA256.HashData(lookupKey, Encoding.UTF8.GetBytes(purpose + ":" + value)));

    public static bool Equal(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    public string Protect(byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];
        using var aes = new AesGcm(encryptionKey, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes(keyVersion));
        return keyVersion + "." + Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]);
    }

    public byte[] Unprotect(string protectedValue)
    {
        var separator = protectedValue.IndexOf('.');
        if (separator < 1 || protectedValue[..separator] != keyVersion)
            throw new CryptographicException("Unsupported key version.");
        var payload = Convert.FromBase64String(protectedValue[(separator + 1)..]);
        var plaintext = new byte[payload.Length - 28];
        using var aes = new AesGcm(encryptionKey, 16);
        aes.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), plaintext,
            Encoding.UTF8.GetBytes(keyVersion));
        return plaintext;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
