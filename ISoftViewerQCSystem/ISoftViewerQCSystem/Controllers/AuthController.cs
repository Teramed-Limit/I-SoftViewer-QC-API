using ISoftViewerLibrary.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeraLinkaAuth.Abstractions;
using TeraLinkaAuth.Authentication;
using TeraLinkaAuth.Contracts;
using TeraLinkaAuth.Contracts.Management;
using TeraLinkaAuth.Extensions;

namespace ISoftViewerQCSystem.Controllers;

/// <summary>
/// 認證控制器，處理登入、登出和 Token 刷新
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly TokenRefreshService _tokenRefreshService;
    private readonly ITokenService _tokenService;

    public AuthController(
        IAuthenticationService authService,
        TokenRefreshService tokenRefreshService,
        ITokenService tokenService)
    {
        _authService = authService;
        _tokenRefreshService = tokenRefreshService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="request">登入請求</param>
    /// <returns>登入結果，包含 Token 和功能列表</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientInfo = GetClientInfo();

        var result = await _authService.LoginAsync(
            request.Username,
            request.Password,
            clientInfo,
            request.RememberMe);

        if (!result.Success)
        {
            return Unauthorized(new ErrorResponse(result.ErrorCode!, result.ErrorMessage!));
        }

        // 將 Functions 轉換為舊格式的 QCFunction 列表（保持向後相容性）
        var functionList = result.Functions
            .ToList();

        return Ok(new AuthLoginResponse
        {
            UserName = result.Username!,
            FunctionList = functionList,
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = result.ExpiresIn,
            RefreshExpiresIn = result.RefreshExpiresIn,
        });
    }

    /// <summary>
    /// 使用 Refresh Token 刷新 Access Token
    /// </summary>
    /// <param name="request">刷新請求</param>
    /// <returns>新的 Token</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequest request)
    {
        var clientInfo = GetClientInfo();

        var result = await _tokenRefreshService.RefreshAsync(request.RefreshToken, clientInfo);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ErrorResponse(result.ErrorCode!, result.ErrorMessage!));
        }

        // 從新的 Access Token 解析使用者資訊以保持向後相容性
        var principal = _tokenService.ValidateToken(result.AccessToken!);
        var username = principal?.GetUsername() ?? string.Empty;
        var functions = principal?.GetFunctions() ?? Enumerable.Empty<string>();

        // 將 Functions 轉換為舊格式
        var functionList = functions
            .ToList();

        return Ok(new AuthTokenResponse
        {
            UserName = username,
            FunctionList = functionList,
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = result.ExpiresIn,
            RefreshExpiresIn = result.RefreshExpiresIn
        });
    }

    /// <summary>
    /// 使用者登出
    /// </summary>
    /// <param name="request">登出請求（可選）</param>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] AuthLogoutRequest? request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientInfo = GetClientInfo();

        await _authService.LogoutAsync(
            userId,
            request?.RefreshToken,
            request?.LogoutFromAllDevices ?? false,
            clientInfo);

        return Ok(new { message = "登出成功" });
    }
    
    private ClientInfo GetClientInfo()
    {
        return new ClientInfo
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = Request.Headers.UserAgent.ToString(),
            SourceSystem = "ISoftViewerQCSystem"
        };
    }
}

#region Request DTOs

/// <summary>
/// 登入請求
/// </summary>
public class AuthLoginRequest
{
    /// <summary>
    /// 使用者名稱
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密碼
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 是否記住我（延長 Refresh Token 有效期）
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// 刷新 Token 請求
/// </summary>
public class AuthRefreshRequest
{
    /// <summary>
    /// Refresh Token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 登出請求
/// </summary>
public class AuthLogoutRequest
{
    /// <summary>
    /// Refresh Token（可選）
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 是否從所有裝置登出
    /// </summary>
    public bool LogoutFromAllDevices { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// 登入回應
/// </summary>
public class AuthLoginResponse
{
    /// <summary>
    /// 使用者名稱
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 功能列表
    /// </summary>
    public IReadOnlyList<string> FunctionList { get; set; } = [];

    /// <summary>
    /// Access Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access Token 過期時間（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Refresh Token 過期時間（秒）
    /// </summary>
    public int RefreshExpiresIn { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Token 刷新回應
/// </summary>
public class AuthTokenResponse
{
    /// <summary>
    /// 使用者名稱
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 功能列表
    /// </summary>
    public IReadOnlyList<string> FunctionList { get; set; } = [];

    /// <summary>
    /// Access Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Access Token 過期時間（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Refresh Token 過期時間（秒）
    /// </summary>
    public int RefreshExpiresIn { get; set; }
}

#endregion
