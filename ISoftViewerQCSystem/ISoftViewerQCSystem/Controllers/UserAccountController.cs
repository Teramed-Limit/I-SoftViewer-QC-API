using AutoMapper;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerQCSystem.JWT;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ISoftViewerQCSystem.Services;
using ISoftViewerQCSystem.utils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     使用者帳號控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserAccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly UserAccountService _userService;
        private readonly AuthService _authService;
        private readonly UserRoleService _userRoleService;

        public UserAccountController(
            UserAccountService userService,
            UserRoleService userRoleService,
            IConfiguration configuration,
            IMapper mapper)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _configuration = configuration;
            _mapper = mapper;
        }


        /// <summary>
        ///     使用者登入
        /// </summary>
        /// <param name="request"></param>
        [AllowAnonymous]
        [HttpPost("login")]
        public ActionResult Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest();

            // DB validation
            var loginUser = _userService.IsAnExistingUser(request.UserName);
            if (loginUser == null)
                return Unauthorized("Unrecognized user, please contact administrator for more information.");

            var functionList = loginUser.UserID == "admin"
                ? _userRoleService.GetAllFunctionList()
                : _userRoleService.GetRoleFunctionList(loginUser);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.UserName)
                // new Claim(ClaimTypes.Role, role)
            };

            return Ok(new LoginResult
            {
                UserName = request.UserName,
                FunctionList = functionList.ToList(),
                AccessToken = _authService.GenerateJwtToken(request.UserName),
                RefreshToken = _userService.GenerateRefreshToken(request.UserName),
            });
        }

        /// <summary>
        ///     使用者登出
        /// </summary>
        [HttpPost("logout")]
        public ActionResult Logout()
        {
            // optionally "revoke" JWT token on the server side --> add the current token to a block-list
            // https://github.com/auth0/node-jsonwebtoken/issues/375

            var userName = User.Identity?.Name;
            _userService.ClearRefreshToken(userName);
            return Ok();
        }

        /// <summary>
        ///     權杖重新取得
        /// </summary>
        /// <param name="request"></param>
        [HttpPost("refreshtoken")]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userName = User.Identity?.Name;

                // 驗證輸入的刷新令牌
                var isValidRefreshToken = _userService.ValidateRefreshTokenAsync(userName, request.RefreshToken);

                if (!isValidRefreshToken)
                {
                    return BadRequest("Invalid refresh token");
                }

                var refreshUser = _userService.GetUserData(userName);

                var functionList = refreshUser.UserID == "admin"
                    ? _userRoleService.GetAllFunctionList()
                    : _userRoleService.GetRoleFunctionList(refreshUser);

                return Ok(new LoginResult
                {
                    UserName = userName,
                    FunctionList = functionList.ToList(),
                    AccessToken = _authService.GenerateJwtToken(userName),
                    RefreshToken = _userService.GenerateRefreshToken(userName),
                });
            }
            catch (SecurityTokenException e)
            {
                return
                    Unauthorized(e.Message); // return 401 so that the client side can redirect the user to login page
            }
        }

        /// <summary>
        ///     取得所有帳號資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IEnumerable<LoginUserDataDto>> Get()
        {
            var userList = _userService.GetAll();
            var userDto = _mapper.Map<IEnumerable<LoginUserDataDto>>(userList);
            return Ok(userDto);
        }

        /// <summary>
        ///     新增使用者帳號資訊
        /// </summary>
        /// <param name="data"></param>
        [HttpPost("userId")]
        public ActionResult AddUser([FromBody] LoginUserDataDto data)
        {
            if (_userService.IsAnExistingUser(data.UserID) != null)
                return BadRequest("Existing user, changing user id.");

            var userList = _mapper.Map<LoginUserData>(data);

            var identityName = User.Identity?.Name;
            _userService.GenerateNewTransaction();
            if (!_userService.AddOrUpdate(userList, identityName)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     修改使用者帳號資訊
        /// </summary>
        /// <param name="data"></param>
        [HttpPost("userId/{userId}")]
        public ActionResult Post([FromBody] LoginUserDataDto data)
        {
            var identityName = User.Identity?.Name;
            var userList = _mapper.Map<LoginUserData>(data);
            if (!_userService.AddOrUpdate(userList, identityName)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     刪除單一帳號
        /// </summary>
        /// <param name="userId"></param>
        [HttpDelete("userId/{userId}")]
        public ActionResult Delete(string userId)
        {
            if (!_userService.Delete(userId)) return BadRequest();
            return Ok();
        }
    }
}