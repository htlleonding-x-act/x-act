using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using XActBackend.Core.Services;

namespace XActBackend.Util;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected static ObjectResult DomainErrorResult(DomainError error)
    {
        int statusCode = error.Code switch
        {
            DomainErrorCodes.InvalidMemberIdentity => StatusCodes.Status400BadRequest,
            DomainErrorCodes.TeamNotInSession => StatusCodes.Status400BadRequest,
            DomainErrorCodes.HostUserDeleted => StatusCodes.Status409Conflict,
            DomainErrorCodes.HostUserAlreadyHasActiveSession => StatusCodes.Status409Conflict,
            DomainErrorCodes.JoinCodeInUse => StatusCodes.Status409Conflict,
            DomainErrorCodes.InvalidSessionTransition => StatusCodes.Status409Conflict,
            DomainErrorCodes.SessionNotJoinable => StatusCodes.Status409Conflict,
            DomainErrorCodes.SessionNotActive => StatusCodes.Status409Conflict,
            DomainErrorCodes.SessionNotFinished => StatusCodes.Status409Conflict,
            DomainErrorCodes.MrXTeamAlreadyExists => StatusCodes.Status409Conflict,
            DomainErrorCodes.CatchingTeamNotEligible => StatusCodes.Status409Conflict,
            DomainErrorCodes.TeamHasMembers => StatusCodes.Status409Conflict,
            DomainErrorCodes.UserDeleted => StatusCodes.Status409Conflict,
            DomainErrorCodes.UserAlreadyJoined => StatusCodes.Status409Conflict,
            DomainErrorCodes.UsernameTaken => StatusCodes.Status409Conflict,
            DomainErrorCodes.TeamLeaderAlreadyExists => StatusCodes.Status409Conflict,
            DomainErrorCodes.PowerUpNotAllowedForTeamRole => StatusCodes.Status409Conflict,
            DomainErrorCodes.GeofencePointLimitReached => StatusCodes.Status422UnprocessableEntity,
            DomainErrorCodes.ChatNotTeamMember => StatusCodes.Status403Forbidden,
            DomainErrorCodes.ReportTargetIsHost => StatusCodes.Status403Forbidden,
            DomainErrorCodes.ReportTargetIsSelf => StatusCodes.Status400BadRequest,
            DomainErrorCodes.ReportVoteAlreadyActive => StatusCodes.Status409Conflict,
            DomainErrorCodes.ReportAlreadyVoted => StatusCodes.Status409Conflict,
            DomainErrorCodes.ReportVoteNotOpen => StatusCodes.Status409Conflict,
            DomainErrorCodes.ReportNotHost => StatusCodes.Status403Forbidden,
            DomainErrorCodes.ReportCancelNotAllowed => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "Domain rule violation",
            Detail = error.Message,
        };
        problemDetails.Extensions["code"] = error.Code;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
        };
    }

    protected static bool ValidateRequest<TValidator, TRequest>(TRequest request)
    where TValidator : AbstractValidator<TRequest>, new()
    where TRequest : notnull =>
    ValidateRequestInternal<TValidator, TRequest>(request, false, out _);
    
    protected static bool ValidateRequest<TValidator, TRequest>(TRequest request, out string[]? validationErrors)
    where TValidator : AbstractValidator<TRequest>, new()
    where TRequest : notnull =>
    ValidateRequestInternal<TValidator, TRequest>(request, true, out validationErrors);
    
    protected static bool ValidateRequest<TValidator, TRequest>(TRequest request, TValidator validator)
    where TValidator : AbstractValidator<TRequest>
    where TRequest : notnull =>
    ValidateRequestInternal(request, validator, false, out _);
    
    protected static bool ValidateRequest<TValidator, TRequest>(TRequest request, TValidator validator,
                                                                out string[]? validationErrors)
    where TValidator : AbstractValidator<TRequest>
    where TRequest : notnull =>
    ValidateRequestInternal(request, validator, true, out validationErrors);
    
    private static bool ValidateRequestInternal<TValidator, TRequest>(TRequest request, bool provideErrors,
                                                                      out string[]? validationErrors)
    where TValidator : AbstractValidator<TRequest>, new()
    where TRequest : notnull
    {
        var validator = new TValidator();
        
        return ValidateRequestInternal(request, validator, provideErrors, out validationErrors);
    }
    
    private static bool ValidateRequestInternal<TValidator, TRequest>(TRequest request, TValidator validator,
                                                                      bool provideErrors,
                                                                      out string[]? validationErrors)
    where TValidator : AbstractValidator<TRequest>
    where TRequest : notnull
    {
        var valRes = validator.Validate(request);
        if (valRes.IsValid)
        {
            validationErrors = null;
            
            return true;
        }
        
        validationErrors = provideErrors ? FormatValidationErrors(valRes.Errors) : null;
        
        return false;
        
        static string[] FormatValidationErrors(IEnumerable<ValidationFailure> errors)
        {
            return errors.Select(FormatValidationError).ToArray();
            
            static string FormatValidationError(ValidationFailure error) =>
            $"{error.PropertyName}: {error.ErrorMessage}";
        }
    }
}
