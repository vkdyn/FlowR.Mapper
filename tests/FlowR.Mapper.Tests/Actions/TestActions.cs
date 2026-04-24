using FlowR.Mapper;
using FlowR.Mapper.Core;
using FlowR.Mapper.Interfaces;
using FlowR.Mapper.Tests.Models;

namespace FlowR.Mapper.Tests.Actions;

// ======================================================
// Mapping Actions for Testing
// ======================================================

/// <summary>
/// Test audit action that tracks execution.
/// </summary>
public class TestAuditAction : IMappingAction<UserEntity, UserDto>
{
    public bool WasExecuted { get; private set; }

    public void Process(UserEntity source, UserDto destination, ResolutionContext context)
    {
        WasExecuted = true;
        destination.Email = "audited@test.com";
    }
}

/// <summary>
/// Post-mapping audit action.
/// </summary>
public class PostMappingAuditAction : IMappingAction<UserEntity, UserDto>
{
    public DateTime ExecutedAt { get; private set; }

    public void Process(UserEntity source, UserDto destination, ResolutionContext context)
    {
        ExecutedAt = DateTime.UtcNow;
        destination.Email = $"processed_{source.Email}";
    }
}

/// <summary>
/// Action that validates before mapping.
/// </summary>
public class ValidationAction : IMappingAction<UserEntity, UserDto>
{
    public void Process(UserEntity source, UserDto destination, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source.Email))
        {
            throw new InvalidOperationException("Email is required");
        }
    }
}
