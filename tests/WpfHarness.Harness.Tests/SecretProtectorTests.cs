using WpfHarness.Harness.Abstractions;
using WpfHarness.Harness.Models;
using WpfHarness.Harness.Storage;
using WpfHarness.Harness.Tools;

namespace WpfHarness.Harness.Tests;

public class SecretProtectorTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrip_RestoresPlaintext() =>
        Assert.Equal("sk-test-key", SecretProtector.Decrypt(SecretProtector.Encrypt("sk-test-key")));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_Empty_ReturnsEmpty(string? plaintext) =>
        Assert.Equal(string.Empty, SecretProtector.Encrypt(plaintext!));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-base64!!!")]
    public void Decrypt_InvalidOrEmpty_ReturnsEmpty(string? ciphertext) =>
        Assert.Equal(string.Empty, SecretProtector.Decrypt(ciphertext));
}
