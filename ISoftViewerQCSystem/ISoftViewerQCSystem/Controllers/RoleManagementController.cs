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
/// Controller for role management CRUD operations.
/// Demonstrates how to use IRoleManagementService.
/// </summary>
[ApiController]
[RequireFunction("ACCOUNT_MAINTAIN")]
[Route("api/[controller]/roles")]
[Authorize]
public class RoleManagementController : ControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public RoleManagementController(IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    /// <summary>
    /// Create a new role.
    /// </summary>
    /// <remarks>
    /// Role codes must be uppercase alphanumeric with underscores (e.g., "ADMIN", "USER_MANAGER").
    /// Role codes are automatically normalized to uppercase.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleManagementService.CreateAsync(request, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return CreatedAtAction(nameof(GetRole), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Get a role by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a paginated list of roles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var options = new RoleQueryOptions
        {
            SearchTerm = search,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };

        var result = await _roleManagementService.GetListAsync(options, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Update a role.
    /// </summary>
    /// <remarks>
    /// Note: Role codes cannot be changed after creation.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleManagementService.UpdateAsync(id, request, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_201"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a role.
    /// </summary>
    /// <remarks>
    /// Roles with user assignments cannot be deleted. Remove all user assignments first.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _roleManagementService.DeleteAsync(id, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_201"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    private ClientInfo GetClientInfo()
    {
        return new ClientInfo
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
    }
}
