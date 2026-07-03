using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using BikeMate.Core.DTOs;

namespace BikeMate.WebAdmin.Services;

public interface IWebAdminAgoraCallService
{
    EmergencyCallSessionDto CreateEmergencyCallSession(int requestId, string? adminIdentity, DateTime startedAt);
}

public sealed class WebAdminAgoraCallService(
    IConfiguration configuration,
    ILogger<WebAdminAgoraCallService> logger) : IWebAdminAgoraCallService
{
    public EmergencyCallSessionDto CreateEmergencyCallSession(int requestId, string? adminIdentity, DateTime startedAt)
    {
        var appId = configuration["Agora:AppId"];
        var certificate = AppCertificate();
        var channelName = $"bikemate-emergency-{requestId}";
        var uid = AdminUid(requestId, adminIdentity);
        var tokenLifetimeSeconds = TokenLifetimeSeconds();
        var expiresAt = startedAt.AddSeconds(tokenLifetimeSeconds);

        if (!IsAgoraId(appId) || !IsAgoraId(certificate))
        {
            logger.LogWarning("WebAdmin Agora session requested for emergency request {RequestId}, but Agora config is missing or invalid.", requestId);
            return new EmergencyCallSessionDto(
                requestId,
                "ConfigurationMissing",
                startedAt,
                null,
                "Agora is not configured. Set Agora:AppId and an Agora certificate in WebAdmin configuration.",
                appId,
                channelName,
                uid,
                null,
                expiresAt);
        }

        var token = AgoraRtcTokenBuilder.BuildTokenWithUid(
            appId!,
            certificate!,
            channelName,
            uid,
            tokenLifetimeSeconds,
            tokenLifetimeSeconds);

        return string.IsNullOrWhiteSpace(token)
            ? new EmergencyCallSessionDto(
                requestId,
                "TokenUnavailable",
                startedAt,
                null,
                "Agora token generation failed. Check the App ID and certificate.",
                appId,
                channelName,
                uid,
                null,
                expiresAt)
            : new EmergencyCallSessionDto(
                requestId,
                "TokenReady",
                startedAt,
                null,
                "Agora Web session is ready.",
                appId,
                channelName,
                uid,
                token,
                expiresAt);
    }

    private uint TokenLifetimeSeconds()
    {
        var configured = configuration.GetValue<int?>("Agora:TokenLifetimeSeconds") ?? 1800;
        return (uint)Math.Clamp(configured, 60, 86400);
    }

    private string? AppCertificate()
    {
        return configuration["Agora:PrimaryCertificate"]
            ?? configuration["Agora:AppCertificate"]
            ?? configuration["Agora:SecondaryCertificate"];
    }

    private static uint AdminUid(int requestId, string? adminIdentity)
    {
        var identity = string.IsNullOrWhiteSpace(adminIdentity) ? "webadmin" : adminIdentity.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{identity}:{requestId}"));
        var raw = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        return 1_000_000_000u + raw % 1_000_000_000u;
    }

    private static bool IsAgoraId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Length == 32 &&
            value.All(Uri.IsHexDigit) &&
            !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
    }

    private static class AgoraRtcTokenBuilder
    {
        private const ushort ServiceTypeRtc = 1;
        private const ushort PrivilegeJoinChannel = 1;
        private const ushort PrivilegePublishAudioStream = 2;
        private const ushort PrivilegePublishVideoStream = 3;
        private const ushort PrivilegePublishDataStream = 4;

        public static string BuildTokenWithUid(
            string appId,
            string appCertificate,
            string channelName,
            uint uid,
            uint tokenExpireSeconds,
            uint privilegeExpireSeconds)
        {
            var issueTs = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var salt = (uint)RandomNumberGenerator.GetInt32(1, 99_999_999);
            var appCertificateBytes = Encoding.UTF8.GetBytes(appCertificate);
            var signing = Hmac(PackUInt32(issueTs), appCertificateBytes);
            signing = Hmac(PackUInt32(salt), signing);

            var serviceRtc = PackServiceRtc(channelName, uid, privilegeExpireSeconds);
            var signingInfo = Concat(
                PackString(Encoding.UTF8.GetBytes(appId)),
                PackUInt32(issueTs),
                PackUInt32(tokenExpireSeconds),
                PackUInt32(salt),
                PackUInt16(1),
                serviceRtc);

            var signature = Hmac(signing, signingInfo);
            var content = Concat(PackString(signature), signingInfo);
            return "007" + Convert.ToBase64String(Compress(content));
        }

        private static byte[] PackServiceRtc(string channelName, uint uid, uint privilegeExpireSeconds)
        {
            var privileges = new SortedDictionary<ushort, uint>
            {
                [PrivilegeJoinChannel] = privilegeExpireSeconds,
                [PrivilegePublishAudioStream] = privilegeExpireSeconds,
                [PrivilegePublishVideoStream] = privilegeExpireSeconds,
                [PrivilegePublishDataStream] = privilegeExpireSeconds
            };

            return Concat(
                PackUInt16(ServiceTypeRtc),
                PackPrivilegeMap(privileges),
                PackString(Encoding.UTF8.GetBytes(channelName)),
                PackString(Encoding.UTF8.GetBytes(uid == 0 ? string.Empty : uid.ToString())));
        }

        private static byte[] PackPrivilegeMap(SortedDictionary<ushort, uint> privileges)
        {
            using var buffer = new MemoryStream();
            buffer.Write(PackUInt16((ushort)privileges.Count));
            foreach (var privilege in privileges)
            {
                buffer.Write(PackUInt16(privilege.Key));
                buffer.Write(PackUInt32(privilege.Value));
            }

            return buffer.ToArray();
        }

        private static byte[] PackString(byte[] value)
        {
            return Concat(PackUInt16((ushort)value.Length), value);
        }

        private static byte[] PackUInt16(ushort value)
        {
            var bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
            return bytes;
        }

        private static byte[] PackUInt32(uint value)
        {
            var bytes = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            return bytes;
        }

        private static byte[] Hmac(byte[] key, byte[] value)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(value);
        }

        private static byte[] Compress(byte[] value)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                zlib.Write(value, 0, value.Length);
            }

            return output.ToArray();
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var length = parts.Sum(x => x.Length);
            var output = new byte[length];
            var offset = 0;
            foreach (var part in parts)
            {
                Buffer.BlockCopy(part, 0, output, offset, part.Length);
                offset += part.Length;
            }

            return output;
        }
    }
}
