using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeraLinkaAuth.Abstractions;
using TeraLinkaAuth.Authorization;
using TeraLinkaAuth.Contracts;
using TeraLinkaAuth.Contracts.Management;
using TeraLinkaAuth.Extensions;
using TeraLinkaAuth.Management;

namespace ISoftViewerQCSystem.Controllers;

/// <summary>
/// Controller for user-role and role-function assignment operations.
/// Uses ILoginUserRoleService for string UserId (LoginUserData) and IRoleFunctionService.
/// </summary>
[ApiController]
[RequireFunction("ACCOUNT_MAINTAIN")]
[Route("api/[controller]")]
[Authorize]
// [RequireRole("ADMIN")] // Only admins can manage assignments
public class AssignmentController : ControllerBase
{
    private readonly ILoginUserRoleService _userRoleService;
    private readonly IRoleFunctionService _roleFunctionService;

    public AssignmentController(
        ILoginUserRoleService userRoleService,
        IRoleFunctionService roleFunctionService)
    {
        _userRoleService = userRoleService;
        _roleFunctionService = roleFunctionService;
    }

    #region User-Role Assignments

    /// <summary>
    /// Get all roles assigned to a user.
    /// </summary>
    [HttpGet("users/{userId}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserRoles(string userId, CancellationToken cancellationToken)
    {
        var result = await _userRoleService.GetUserRolesAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Assign a role to a user.
    /// </summary>
    /// <remarks>
    /// This operation is idempotent - assigning an already assigned role returns success.
    /// </remarks>
    [HttpPost("users/{userId}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRoleToUser(string userId, Guid roleId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userRoleService.AssignRoleAsync(userId, roleId, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    /// <summary>
    /// Remove a role from a user.
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnassignRoleFromUser(string userId, Guid roleId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userRoleService.UnassignRoleAsync(userId, roleId, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }
    
    /// <summary>
    /// Sync multiple roles to a user in a single atomic operation.
    /// </summary>
    /// <remarks>
    /// If any role assignment fails, all assignments are rolled back.
    /// Already assigned roles are counted as successful (idempotent).
    /// </remarks>
    [HttpPut("users/{userId}/roles")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SyncRolesToUser(string userId, [FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userRoleService.SyncUserRolesAsync(userId, request.Ids, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_101"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Assign multiple roles to a user in a single atomic operation.
    /// </summary>
    /// <remarks>
    /// If any role assignment fails, all assignments are rolled back.
    /// Already assigned roles are counted as successful (idempotent).
    /// </remarks>
    [HttpPost("users/{userId}/roles/bulk")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRolesToUser(string userId, [FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userRoleService.AssignRolesAsync(userId, request.Ids, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_101"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Remove multiple roles from a user in a single atomic operation.
    /// </summary>
    [HttpDelete("users/{userId}/roles/bulk")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnassignRolesFromUser(string userId, [FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userRoleService.UnassignRolesAsync(userId, request.Ids, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all users assigned to a role (paginated).
    /// </summary>
    [HttpGet("roles/{roleId:guid}/users")]
    [ProducesResponseType(typeof(PagedResult<LoginUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoleUsers(
        Guid roleId,
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var options = new LoginUserQueryOptions
        {
            SearchTerm = search,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };

        var result = await _userRoleService.GetRoleUsersAsync(roleId, options, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    #endregion

    #region Role-Function Assignments

    /// <summary>
    /// Get all functions assigned to a role.
    /// </summary>
    [HttpGet("roles/{roleId:guid}/functions")]
    [ProducesResponseType(typeof(IReadOnlyList<FunctionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoleFunctions(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _roleFunctionService.GetRoleFunctionsAsync(roleId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Assign a function to a role.
    /// </summary>
    /// <remarks>
    /// This operation is idempotent - assigning an already assigned function returns success.
    /// </remarks>
    [HttpPost("roles/{roleId:guid}/functions/{functionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignFunctionToRole(Guid roleId, Guid functionId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleFunctionService.AssignFunctionAsync(roleId, functionId, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    /// <summary>
    /// Remove a function from a role.
    /// </summary>
    [HttpDelete("roles/{roleId:guid}/functions/{functionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnassignFunctionFromRole(Guid roleId, Guid functionId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleFunctionService.UnassignFunctionAsync(roleId, functionId, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    /// <summary>
    /// Assign multiple functions to a role in a single atomic operation.
    /// </summary>
    /// <remarks>
    /// If any function assignment fails, all assignments are rolled back.
    /// Already assigned functions are counted as successful (idempotent).
    /// </remarks>
    [HttpPut("roles/{roleId:guid}/functions")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SyncFunctionsToRole(Guid roleId, [FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleFunctionService.SyncRoleFunctionsAsync(roleId, request.Ids, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_201"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }
    
    /// <summary>
    /// Assign multiple functions to a role in a single atomic operation.
    /// </summary>
    /// <remarks>
    /// If any function assignment fails, all assignments are rolled back.
    /// Already assigned functions are counted as successful (idempotent).
    /// </remarks>
    [HttpPost("roles/{roleId:guid}/functions/bulk")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignFunctionsToRole(Guid roleId, [FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleFunctionService.AssignFunctionsAsync(roleId, request.Ids, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_201"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Remove multiple functions from a role in a single atomic operation.
    /// </summary>
    [HttpDelete("roles/{roleId:guid}/functions/bulk")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnassignFunctionsFromRole(Guid roleId, [FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleFunctionService.UnassignFunctionsAsync(roleId, request.Ids, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all roles that have a specific function assigned.
    /// </summary>
    [HttpGet("functions/{functionId:guid}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFunctionRoles(Guid functionId, CancellationToken cancellationToken)
    {
        var result = await _roleFunctionService.GetFunctionRolesAsync(functionId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    #endregion

    private ClientInfo GetClientInfo()
    {
        return new ClientInfo
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
    }
}

// Request DTOs
public record BulkAssignRequest(IEnumerable<Guid> Ids);
