using AwesomeAssertions;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;

namespace XActBackend.Test;

public sealed class DomainErrorTests
{
    [Fact]
    public void HostUserDeleted_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.HostUserDeleted(5);

        result.Code.Should().Be(DomainErrorCodes.HostUserDeleted);
        result.Message.Should().Contain("5");
    }

    [Fact]
    public void HostUserAlreadyHasActiveSession_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.HostUserAlreadyHasActiveSession(5);

        result.Code.Should().Be(DomainErrorCodes.HostUserAlreadyHasActiveSession);
        result.Message.Should().Contain("5");
    }

    [Fact]
    public void JoinCodeInUse_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.JoinCodeInUse("ABC123");

        result.Code.Should().Be(DomainErrorCodes.JoinCodeInUse);
        result.Message.Should().Contain("ABC123");
    }

    [Fact]
    public void InvalidSessionTransition_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.InvalidSessionTransition(SessionStatus.Waiting, SessionStatus.Finished);

        result.Code.Should().Be(DomainErrorCodes.InvalidSessionTransition);
        result.Message.Should().Contain("Waiting").And.Contain("Finished");
    }

    [Fact]
    public void SessionNotJoinable_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.SessionNotJoinable(3, SessionStatus.Active);

        result.Code.Should().Be(DomainErrorCodes.SessionNotJoinable);
        result.Message.Should().Contain("3").And.Contain("Active");
    }

    [Fact]
    public void SessionNotActive_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.SessionNotActive(3, SessionStatus.Waiting);

        result.Code.Should().Be(DomainErrorCodes.SessionNotActive);
        result.Message.Should().Contain("3").And.Contain("Waiting");
    }

    [Fact]
    public void MrXTeamAlreadyExists_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.MrXTeamAlreadyExists(3);

        result.Code.Should().Be(DomainErrorCodes.MrXTeamAlreadyExists);
        result.Message.Should().Contain("3");
    }

    [Fact]
    public void TeamHasMembers_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.TeamHasMembers(8);

        result.Code.Should().Be(DomainErrorCodes.TeamHasMembers);
        result.Message.Should().Contain("8");
    }

    [Fact]
    public void InvalidMemberIdentity_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.InvalidMemberIdentity();

        result.Code.Should().Be(DomainErrorCodes.InvalidMemberIdentity);
        result.Message.Should().Contain("either").And.Contain("not both");
    }

    [Fact]
    public void TeamNotInSession_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.TeamNotInSession(4, 7);

        result.Code.Should().Be(DomainErrorCodes.TeamNotInSession);
        result.Message.Should().Contain("4").And.Contain("7");
    }

    [Fact]
    public void UserDeleted_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.UserDeleted(9);

        result.Code.Should().Be(DomainErrorCodes.UserDeleted);
        result.Message.Should().Contain("9");
    }

    [Fact]
    public void UserAlreadyJoined_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.UserAlreadyJoined(9, 2);

        result.Code.Should().Be(DomainErrorCodes.UserAlreadyJoined);
        result.Message.Should().Contain("9").And.Contain("2");
    }

    [Fact]
    public void TeamLeaderAlreadyExists_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.TeamLeaderAlreadyExists(12);

        result.Code.Should().Be(DomainErrorCodes.TeamLeaderAlreadyExists);
        result.Message.Should().Contain("12");
    }

    [Fact]
    public void PowerUpNotAllowedForTeamRole_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.PowerUpNotAllowedForTeamRole(PowerUpType.BlackTicket, TeamRole.Detective);

        result.Code.Should().Be(DomainErrorCodes.PowerUpNotAllowedForTeamRole);
        result.Message.Should().Contain("BlackTicket").And.Contain("Detective");
    }

    [Fact]
    public void GeofencePointLimitReached_ReturnsExpectedCodeAndMessage()
    {
        var result = DomainError.GeofencePointLimitReached(3, 10);

        result.Code.Should().Be(DomainErrorCodes.GeofencePointLimitReached);
        result.Message.Should().Contain("3").And.Contain("10");
    }
}
