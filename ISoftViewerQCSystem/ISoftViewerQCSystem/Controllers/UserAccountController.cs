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
/// 使用者帳號管理控制器
/// 使用 TeraLinkaAuth 的 ILoginUserManagementService 進行用戶管理
/// </summary>
[ApiController]
[RequireFunction("ACCOUNT_MAINTAIN")]
[Route("api/[controller]")]
[Authorize]
public class UserAccountController : ControllerBase
{
    private readonly ILoginUserManagementService _userManagementService;

    public UserAccountController(ILoginUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// 取得所有帳號資訊 (分頁)
    /// </summary>
    /// <param name="search">搜尋關鍵字</param>
    /// <param name="includeInactive">是否包含停用帳號</param>
    /// <param name="page">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>使用者列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LoginUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsers(
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

        var result = await _userManagementService.GetListAsync(options, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// 取得單一帳號資訊
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>使用者資訊</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(LoginUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUser(string userId, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetByIdAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 新增使用者帳號
    /// </summary>
    /// <param name="request">使用者資料</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建立的使用者資訊</returns>
    [HttpPost]
    [ProducesResponseType(typeof(LoginUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser([FromBody] CreateLoginUserRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userManagementService.CreateAsync(request, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return CreatedAtAction(nameof(GetUser), new { userId = result.Value!.UserId }, result.Value);
    }

    /// <summary>
    /// 修改使用者帳號資訊
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="request">使用者資料</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新後的使用者資訊</returns>
    [HttpPut("{userId}")]
    [ProducesResponseType(typeof(LoginUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateLoginUserRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userManagementService.UpdateAsync(userId, request, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_LOGIN_101"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 修改使用者密碼
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="request">密碼變更請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPut("{userId}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePassword(string userId, [FromBody] UpdatePasswordRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userManagementService.UpdatePasswordAsync(userId, request.NewPassword, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_LOGIN_101"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    /// <summary>
    /// 刪除單一帳號 (軟刪除，設定 IsActive = false)
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userManagementService.DeleteAsync(userId, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_LOGIN_101"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    /// <summary>
    /// 恢復已刪除的帳號
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost("{userId}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreUser(string userId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userManagementService.RestoreAsync(userId, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "AUTH_LOGIN_101"
                ? NotFound(new ErrorResponse(result.ErrorCode, result.ErrorMessage))
                : BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return NoContent();
    }

    /// <summary>
    /// 取得目前登入使用者的資訊
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>目前使用者資訊</returns>
    [HttpGet("me")]
    [SkipFunctionAuthorization]
    [ProducesResponseType(typeof(LoginUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var result = await _userManagementService.GetByIdAsync(currentUserId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 目前使用者修改自己的密碼
    /// </summary>
    /// <param name="request">密碼變更請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPut("me/password")]
    [SkipFunctionAuthorization]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyPassword([FromBody] UpdatePasswordRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var clientInfo = GetClientInfo();
        var result = await _userManagementService.UpdatePasswordAsync(currentUserId, request.NewPassword, currentUserId, clientInfo, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse(result.ErrorCode!, result.ErrorMessage));
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

/// <summary>
/// 更新密碼請求
/// </summary>
/// <param name="NewPassword">新密碼 (至少 8 個字元)</param>
public record UpdatePasswordRequest(string NewPassword);
