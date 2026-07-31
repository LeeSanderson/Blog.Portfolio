using System.Security.Cryptography;
using System.Text;

namespace Blog.Portfolio.Apps.EmailSubscription.Backend.Tokens;

public sealed class HmacSubscriberTokenService : ISubscriberTokenService
{
    private readonly byte[] _signingKey;

    public HmacSubscriberTokenService(string signingKey) => _signingKey = Encoding.UTF8.GetBytes(signingKey);

    public string CreateSignature(Guid subscriberId, TokenPurpose purpose)
    {
        var payload = Encoding.UTF8.GetBytes($"{subscriberId:N}:{purpose}");
        var hash = HMACSHA256.HashData(_signingKey, payload);
        return Convert.ToHexString(hash);
    }

    public bool IsValid(Guid subscriberId, TokenPurpose purpose, string signature)
    {
        var expectedSignature = CreateSignature(subscriberId, purpose);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(signature));
    }
}
