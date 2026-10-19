using PMS.Infrastructure.Services;

namespace PMS.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_Verify_accepts_the_right_password()
    {
        var hash = _hasher.Hash("correct horse 42");
        Assert.True(_hasher.Verify("correct horse 42", hash));
    }

    [Fact]
    public void Verify_rejects_a_wrong_password()
    {
        var hash = _hasher.Hash("correct horse 42");
        Assert.False(_hasher.Verify("correct horse 43", hash));
        Assert.False(_hasher.Verify("", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plaintext-not-a-hash")]
    [InlineData("PBKDF2-SHA256$abc$AAAA$AAAA")]
    public void Verify_never_accepts_a_missing_or_malformed_stored_hash(string? stored)
    {
        Assert.False(_hasher.Verify("anything", stored));
    }

    [Fact]
    public void Hash_is_salted_and_never_contains_the_password()
    {
        var a = _hasher.Hash("same password 1");
        var b = _hasher.Hash("same password 1");
        Assert.NotEqual(a, b);
        Assert.DoesNotContain("same password 1", a);
    }
}
