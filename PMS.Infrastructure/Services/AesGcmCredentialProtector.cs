using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PMS.Application.Common.Interfaces;

namespace PMS.Infrastructure.Services;

/// <summary>
/// AES-256-GCM with a random nonce per value. Payload = "v1:" + base64(nonce | tag | ciphertext).
/// The key comes from configuration (CredentialEncryption:Key, 32 random bytes as base64) - put it
/// in Key Vault / an app setting, never in source. Losing the key makes stored secrets unreadable.
/// </summary>
public sealed class AesGcmCredentialProtector : ICredentialProtector
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly Lazy<byte[]> _key;

    public AesGcmCredentialProtector(IConfiguration configuration)
    {
        // Lazy so the API still boots without the key; only credential calls fail.
        _key = new Lazy<byte[]>(() =>
        {
            var raw = configuration["CredentialEncryption:Key"];
            if (string.IsNullOrWhiteSpace(raw) || raw == "CHANGEME")
                throw new InvalidOperationException(
                    "CredentialEncryption:Key is not configured. Generate one with: " +
                    "[Convert]::ToBase64String((1..32 | % { Get-Random -Max 256 }) -as [byte[]])  (32 random bytes, base64).");
            byte[] key;
            try { key = Convert.FromBase64String(raw); }
            catch (FormatException) { throw new InvalidOperationException("CredentialEncryption:Key must be base64."); }
            if (key.Length != 32) throw new InvalidOperationException("CredentialEncryption:Key must decode to exactly 32 bytes.");
            return key;
        });
    }

    public string Protect(string plaintext)
    {
        var data = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[data.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(_key.Value, TagSize);
        aes.Encrypt(nonce, data, cipher, tag);

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipher.CopyTo(payload, NonceSize + TagSize);
        return Prefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string payload)
    {
        if (!payload.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Unrecognised credential payload format.");
        var bytes = Convert.FromBase64String(payload[Prefix.Length..]);
        if (bytes.Length < NonceSize + TagSize) throw new InvalidOperationException("Credential payload is truncated.");

        var nonce = bytes.AsSpan(0, NonceSize);
        var tag = bytes.AsSpan(NonceSize, TagSize);
        var cipher = bytes.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key.Value, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
