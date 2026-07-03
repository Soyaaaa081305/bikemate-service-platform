using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public int? RequestId { get; set; }

    public string ConversationType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ServiceRequest? Request { get; set; }
}
