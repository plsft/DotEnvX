using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1.Sec;

namespace DotEnvX.Core.Encryption;

public static class DotEnvEncryption
{
    private const string ENCRYPTED_PREFIX = "encrypted:";
    private const int KEY_SIZE = 32; // 256 bits
    
    public static KeyPair GenerateKeyPair()
    {
        var keyGen = new ECKeyPairGenerator();
        var secureRandom = new SecureRandom();
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var keyGenParams = new ECKeyGenerationParameters(domainParams, secureRandom);
        
        keyGen.Init(keyGenParams);
        var keyPair = keyGen.GenerateKeyPair();
        
        var privateKey = ((ECPrivateKeyParameters)keyPair.Private).D.ToByteArrayUnsigned();
        var publicKey = ((ECPublicKeyParameters)keyPair.Public).Q.GetEncoded(false);
        
        return new KeyPair
        {
            PrivateKey = BytesToHex(privateKey),
            PublicKey = BytesToHex(publicKey)
        };
    }
    
    public static string Encrypt(string value, string publicKey)
    {
        try
        {
            var publicKeyBytes = HexToBytes(publicKey);
            var valueBytes = Encoding.UTF8.GetBytes(value);
            
            // Generate ephemeral key pair for ECIES
            var ephemeralKeyPair = GenerateEphemeralKeyPair();
            var ephemeralPrivateKey = HexToBytes(ephemeralKeyPair.PrivateKey);
            var ephemeralPublicKey = HexToBytes(ephemeralKeyPair.PublicKey);
            
            // Perform ECDH to get shared secret
            var sharedSecret = PerformECDH(ephemeralPrivateKey, publicKeyBytes);
            
            // Derive encryption key from shared secret
            var encryptionKey = DeriveKey(sharedSecret);
            
            // Encrypt the value using AES
            var (ciphertext, nonce, tag) = EncryptAES(valueBytes, encryptionKey);
            
            // Combine ephemeral public key, nonce, ciphertext, and tag
            var result = new byte[ephemeralPublicKey.Length + nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(ephemeralPublicKey, 0, result, 0, ephemeralPublicKey.Length);
            Buffer.BlockCopy(nonce, 0, result, ephemeralPublicKey.Length, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, ephemeralPublicKey.Length + nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, ephemeralPublicKey.Length + nonce.Length + ciphertext.Length, tag.Length);
            
            return ENCRYPTED_PREFIX + Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
        }
    }
    
    public static string Decrypt(string encryptedValue, string privateKey)
    {
        try
        {
            if (!encryptedValue.StartsWith(ENCRYPTED_PREFIX))
            {
                return encryptedValue; // Not encrypted
            }
            
            var base64Part = encryptedValue.Substring(ENCRYPTED_PREFIX.Length);
            var encryptedBytes = Convert.FromBase64String(base64Part);
            
            var privateKeyBytes = HexToBytes(privateKey);
            
            // Extract components
            var ephemeralPublicKey = new byte[65]; // Uncompressed public key
            var nonce = new byte[12]; // AES-GCM nonce
            var tag = new byte[16]; // AES-GCM tag
            var ciphertext = new byte[encryptedBytes.Length - ephemeralPublicKey.Length - nonce.Length - tag.Length];
            
            Buffer.BlockCopy(encryptedBytes, 0, ephemeralPublicKey, 0, ephemeralPublicKey.Length);
            Buffer.BlockCopy(encryptedBytes, ephemeralPublicKey.Length, nonce, 0, nonce.Length);
            Buffer.BlockCopy(encryptedBytes, ephemeralPublicKey.Length + nonce.Length, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(encryptedBytes, ephemeralPublicKey.Length + nonce.Length + ciphertext.Length, tag, 0, tag.Length);
            
            // Perform ECDH to get shared secret
            var sharedSecret = PerformECDH(privateKeyBytes, ephemeralPublicKey);
            
            // Derive encryption key from shared secret
            var encryptionKey = DeriveKey(sharedSecret);
            
            // Decrypt the value using AES
            var plaintext = DecryptAES(ciphertext, encryptionKey, nonce, tag);
            
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }
    
    public static bool IsEncrypted(string value)
    {
        return value?.StartsWith(ENCRYPTED_PREFIX) ?? false;
    }
    
    public static string GetPublicKeyFromPrivateKey(string privateKey)
    {
        var privateKeyBytes = HexToBytes(privateKey);
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var privateKeyParams = new ECPrivateKeyParameters(new BigInteger(1, privateKeyBytes), domainParams);
        var publicKeyParams = new ECPublicKeyParameters(domainParams.G.Multiply(privateKeyParams.D), domainParams);
        var publicKeyBytes = publicKeyParams.Q.GetEncoded(false);
        return BytesToHex(publicKeyBytes);
    }
    
    private static KeyPair GenerateEphemeralKeyPair()
    {
        return GenerateKeyPair();
    }
    
    private static byte[] PerformECDH(byte[] privateKey, byte[] publicKey)
    {
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        
        var privateKeyParams = new ECPrivateKeyParameters(new BigInteger(1, privateKey), domainParams);
        var publicKeyPoint = curve.Curve.DecodePoint(publicKey);
        var publicKeyParams = new ECPublicKeyParameters(publicKeyPoint, domainParams);
        
        var agreement = new ECDHBasicAgreement();
        agreement.Init(privateKeyParams);
        var sharedSecret = agreement.CalculateAgreement(publicKeyParams);
        
        return sharedSecret.ToByteArrayUnsigned();
    }
    
    private static byte[] DeriveKey(byte[] sharedSecret)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(sharedSecret);
    }
    
    private static (byte[] ciphertext, byte[] nonce, byte[] tag) EncryptAES(byte[] plaintext, byte[] key)
    {
        using var aesGcm = new AesGcm(key, 16);
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        
        return (ciphertext, nonce, tag);
    }
    
    private static byte[] DecryptAES(byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag)
    {
        using var aesGcm = new AesGcm(key, 16);
        var plaintext = new byte[ciphertext.Length];
        
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        
        return plaintext;
    }
    
    private static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
    
    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hex string must have even length");
            
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
    
    public class KeyPair
    {
        public required string PrivateKey { get; set; }
        public required string PublicKey { get; set; }
    }
}