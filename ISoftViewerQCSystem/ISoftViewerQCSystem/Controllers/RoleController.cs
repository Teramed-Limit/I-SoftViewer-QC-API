using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    /// 角色控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly UserRoleService _userRoleService;

        public RoleController(UserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }
        /// <summary>
        ///     取得所有角色列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IEnumerable<UserRole>> Get()
        {
            return Ok(_userRoleService.GetAll());
        }

        /// <summary>
        ///     新增角色列表
        /// </summary>
        /// <param name="data"></param>
        [HttpPost("roleName")]
        public ActionResult AddUser([FromBody] UserRole data)
        {
            if (!_userRoleService.AddOrUpdate(data)) return BadRequest();
            return Ok();
        }

        /// <summary>
        /// 修改角色列表
        /// </summary>
        /// <param name="data"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        [HttpPost("roleName/{roleName}")]
        public ActionResult Post([FromBody] UserRole data, string roleName)
        {
            if (!_userRoleService.AddOrUpdate(data)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     刪除角色列表
        /// </summary>
        /// <param name="roleName"></param>
        [HttpDelete("roleName/{roleName}")]
        public ActionResult Delete(string roleName)
        {
            if (!_userRoleService.Delete(roleName)) 
                return BadRequest();

            return Ok();
        }

        /// <summary>
        ///     增加指定Role QC function
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="qcFunction"></param>
        [HttpPost("roleName/{roleName}/qcFunction/{qcFunction}")]
        public ActionResult AddQCFuction(string roleName, string qcFunction)
        {
            if (!_userRoleService.AddQCFunction(roleName, qcFunction)) return BadRequest();
            return Ok();
        }

        /// <summary>
        /// 刪除指定Role QC function
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="qcFunction"></param>
        /// <returns></returns>
        [HttpDelete("roleName/{roleName}/qcFunction/{qcFunction}")]
        public ActionResult DeleteQCFunction(string roleName, string qcFunction)
        {
            if (!_userRoleService.DeleteQcFunction(roleName, qcFunction)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     取得指定Role QC function
        /// </summary>
        /// <param name="roleName"></param>
        [HttpGet("roleName/{roleName}/qcFunction")]
        public ActionResult<IEnumerable<RoleFunction>> GetRoleFuctionList(string roleName)
        {
            return Ok(_userRoleService.GetRoleFunctionList(roleName));
        }
    }
}
