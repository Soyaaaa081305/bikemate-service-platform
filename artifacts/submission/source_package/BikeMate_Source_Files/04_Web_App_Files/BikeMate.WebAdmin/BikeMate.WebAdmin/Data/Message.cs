using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Message
{
    public int MessageId { get; set; }

    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string MessageText { get; set; } = null!;

    public string? AttachmentUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;

    public virtual User SenderUser { get; set; } = null!;
}
