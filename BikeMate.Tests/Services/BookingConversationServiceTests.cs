using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Tests.Services;

public sealed class BookingConversationServiceTests : IDisposable
{
    private readonly BikeMateDbContext _db;
    private readonly BookingConversationService _sut;

    public BookingConversationServiceTests()
    {
        var options = new DbContextOptionsBuilder<BikeMateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BikeMateDbContext(options);
        _sut = new BookingConversationService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task EnsureEmergencySupportConversationAsync_DeduplicatesCustomerWhenTheyAreAlsoSystemAdmin()
    {
        var now = DateTime.UtcNow;
        var customerRole = new Role { RoleId = 1, RoleName = AppRoles.Customer };
        var adminRole = new Role { RoleId = 4, RoleName = AppRoles.SystemAdmin };
        var user = new User
        {
            UserId = 10,
            FirstName = "Isaiah",
            LastName = "Noda",
            Email = "isaiahandreinoda@gmail.com",
            AccountStatus = "active",
            CreatedAt = now
        };
        var client = new Client
        {
            ClientId = 20,
            UserId = user.UserId,
            User = user,
            CreatedAt = now
        };
        var request = new ServiceRequest
        {
            RequestId = 30,
            ClientId = client.ClientId,
            Client = client,
            CurrentStatusId = 1,
            IssueDescription = "[EMERGENCY] Roadside help",
            ServiceLocationAddress = "San Pedro",
            CreatedAt = now
        };

        _db.Roles.AddRange(customerRole, adminRole);
        _db.Users.Add(user);
        _db.UserRoles.AddRange(
            new UserRole { UserId = user.UserId, RoleId = customerRole.RoleId, User = user, Role = customerRole, AssignedAt = now },
            new UserRole { UserId = user.UserId, RoleId = adminRole.RoleId, User = user, Role = adminRole, AssignedAt = now });
        _db.Clients.Add(client);
        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync();

        var conversationId = await _sut.EnsureEmergencySupportConversationAsync(request.RequestId, CancellationToken.None);

        Assert.NotNull(conversationId);
        var conversation = await _db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .SingleAsync(x => x.ConversationId == conversationId);
        Assert.Single(conversation.Participants);
        Assert.Equal(user.UserId, conversation.Participants.Single().UserId);
        Assert.Single(conversation.Messages);
    }
}
