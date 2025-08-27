using DotEnvX.Core.Encryption;
using FluentAssertions;

namespace DotEnvX.Tests;

public class DotEnvEncryptionTests
{
    [Fact]
    public void GenerateKeyPair_ReturnsValidKeyPair()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        
        keyPair.Should().NotBeNull();
        keyPair.PrivateKey.Should().NotBeNullOrEmpty();
        keyPair.PublicKey.Should().NotBeNullOrEmpty();
        
        // Keys should be hex strings
        keyPair.PrivateKey.Should().MatchRegex("^[a-f0-9]+$");
        keyPair.PublicKey.Should().MatchRegex("^[a-f0-9]+$");
        
        // Private key should be 64 hex chars (32 bytes)
        keyPair.PrivateKey.Should().HaveLength(64);
        
        // Public key should be 130 hex chars (65 bytes uncompressed)
        keyPair.PublicKey.Should().HaveLength(130);
    }
    
    [Fact]
    public void Encrypt_Decrypt_RoundTrip_Works()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        var originalValue = "This is a secret value!";
        
        var encrypted = DotEnvEncryption.Encrypt(originalValue, keyPair.PublicKey);
        encrypted.Should().StartWith("encrypted:");
        encrypted.Should().NotContain(originalValue);
        
        var decrypted = DotEnvEncryption.Decrypt(encrypted, keyPair.PrivateKey);
        decrypted.Should().Be(originalValue);
    }
    
    [Fact]
    public void Encrypt_DifferentValuesProduceDifferentCiphertext()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        
        var encrypted1 = DotEnvEncryption.Encrypt("value1", keyPair.PublicKey);
        var encrypted2 = DotEnvEncryption.Encrypt("value2", keyPair.PublicKey);
        
        encrypted1.Should().NotBe(encrypted2);
    }
    
    [Fact]
    public void Encrypt_SameValueProducesDifferentCiphertext_DueToRandomness()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        var value = "same value";
        
        var encrypted1 = DotEnvEncryption.Encrypt(value, keyPair.PublicKey);
        var encrypted2 = DotEnvEncryption.Encrypt(value, keyPair.PublicKey);
        
        // Due to random nonce, same value should produce different ciphertext
        encrypted1.Should().NotBe(encrypted2);
        
        // But both should decrypt to the same value
        var decrypted1 = DotEnvEncryption.Decrypt(encrypted1, keyPair.PrivateKey);
        var decrypted2 = DotEnvEncryption.Decrypt(encrypted2, keyPair.PrivateKey);
        
        decrypted1.Should().Be(value);
        decrypted2.Should().Be(value);
    }
    
    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var keyPair1 = DotEnvEncryption.GenerateKeyPair();
        var keyPair2 = DotEnvEncryption.GenerateKeyPair();
        
        var encrypted = DotEnvEncryption.Encrypt("secret", keyPair1.PublicKey);
        
        var action = () => DotEnvEncryption.Decrypt(encrypted, keyPair2.PrivateKey);
        action.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void IsEncrypted_DetectsEncryptedValues()
    {
        DotEnvEncryption.IsEncrypted("encrypted:abc123").Should().BeTrue();
        DotEnvEncryption.IsEncrypted("not encrypted").Should().BeFalse();
        DotEnvEncryption.IsEncrypted("").Should().BeFalse();
        DotEnvEncryption.IsEncrypted(null).Should().BeFalse();
    }
    
    [Fact]
    public void GetPublicKeyFromPrivateKey_ReturnsCorrectPublicKey()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        
        var derivedPublicKey = DotEnvEncryption.GetPublicKeyFromPrivateKey(keyPair.PrivateKey);
        
        derivedPublicKey.Should().Be(keyPair.PublicKey);
    }
    
    [Fact]
    public void Encrypt_EmptyString_Works()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        
        var encrypted = DotEnvEncryption.Encrypt("", keyPair.PublicKey);
        var decrypted = DotEnvEncryption.Decrypt(encrypted, keyPair.PrivateKey);
        
        decrypted.Should().Be("");
    }
    
    [Fact]
    public void Encrypt_LongString_Works()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        var longValue = new string('a', 10000);
        
        var encrypted = DotEnvEncryption.Encrypt(longValue, keyPair.PublicKey);
        var decrypted = DotEnvEncryption.Decrypt(encrypted, keyPair.PrivateKey);
        
        decrypted.Should().Be(longValue);
    }
    
    [Fact]
    public void Encrypt_SpecialCharacters_Works()
    {
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        var specialValue = "!@#$%^&*()_+-=[]{}|;':\",./<>?\n\r\t";
        
        var encrypted = DotEnvEncryption.Encrypt(specialValue, keyPair.PublicKey);
        var decrypted = DotEnvEncryption.Decrypt(encrypted, keyPair.PrivateKey);
        
        decrypted.Should().Be(specialValue);
    }
}