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
/// Controller for function permission management CRUD operations.
/// Demonstrates how to use IFunctionManagementService.
/// </summary>
[ApiController]
[RequireFunction("ACCOUNT_MAINTAIN")]
[Route("api/functions")]
[Authorize]
// [RequireRole("ADMIN")] // Only admins can manage functions
public class FunctionManagementController : ControllerBase
{
    private readonly IFunctionManagementService _functionManagementService;

    public FunctionManagementController(IFunctionManagementService functionManagementService)
    {
        _functionManagementService = functionManagementService;
    }

    /// <summary>
    /// Create a new function permission.
    /// </summary>
    /// <remarks>
    /// Function codes must be uppercase alphanumeric with underscores (e.g., "USER_READ", "REPORT_EXPORT").
    /// Function codes are automatically normalized to uppercase.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(FunctionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateFunction([FromBody] CreateFunctionRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _functionManagementService.CreateAsync(request, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return CreatedAtAction(nameof(GetFunction), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Get a function by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FunctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFunction(Guid id, CancellationToken cancellationToken)
    {
        var result = await _functionManagementService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a paginated list of functions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FunctionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFunctions(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var options = new FunctionQueryOptions
        {
            SearchTerm = search,
            Category = category,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };

        var result = await _functionManagementService.GetListAsync(options, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Update a function.
    /// </summary>
    /// <remarks>
    /// Note: Function codes cannot be changed after creation.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FunctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateFunction(Guid id, [FromBody] UpdateFunctionRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _functionManagementService.UpdateAsync(id, request, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_301"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a function.
    /// </summary>
    /// <remarks>
    /// Functions with role assignments cannot be deleted. Remove all role assignments first.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteFunction(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _functionManagementService.DeleteAsync(id, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_MGMT_301"
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
