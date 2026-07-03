using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public string? ProfileImageUrl { get; set; }

    public bool EmailVerified { get; set; }

    public bool PhoneVerified { get; set; }

    public string AccountStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Client? Client { get; set; }

    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();

    public virtual Mechanic? Mechanic { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<RequestStatusHistory> RequestStatusHistories { get; set; } = new List<RequestStatusHistory>();

    public virtual ICollection<Shop> Shops { get; set; } = new List<Shop>();

    public virtual ICollection<UserAuthProvider> UserAuthProviders { get; set; } = new List<UserAuthProvider>();

    public virtual ICollection<UserDeviceToken> UserDeviceTokens { get; set; } = new List<UserDeviceToken>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
