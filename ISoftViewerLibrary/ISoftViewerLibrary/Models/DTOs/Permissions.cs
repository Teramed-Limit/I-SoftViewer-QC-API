using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs
{    
    public static class Permissions
    {
        #region V1
        public static class V1
        {
            #region UserRole
            /// <summary>
            /// 角色物件
            /// </summary>
            public class UserRole : ICloneable
            {                
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="name"></param>
                /// <param name="description"></param>
                public UserRole(string name, string description)
                {
                    if (name == "")
                        throw new Exception("Role can not empty string");
                    Name = name;
                    Description = description;
                    OpFuncNames = new List<OpFunctionName>();
                }

                public UserRole(string name, string description, List<OpFunctionName> fnames)
                {
                    if (name == "")
                        throw new Exception("Role can not empty string");
                    Name = name;
                    Description = description;
                    OpFuncNames = fnames.ToList();
                }

                #region Fields
                /// <summary>
                /// 角色名稱
                /// </summary>
                [Required]
                public string Name { get; set; }
                /// <summary>
                /// 角色說明
                /// </summary>                
                public string Description { get; set; }
                /// <summary>
                /// 操作功能名稱
                /// </summary>
                [Required]
                public List<OpFunctionName> OpFuncNames { get; set; }
                /// <summary>
                /// 複製副本
                /// </summary>
                /// <returns></returns>
                public object Clone() => new UserRole(Name, Description, OpFuncNames);
                #endregion
            }
            #endregion

            #region UserAccount
            /// <summary>
            /// 使用者帳號
            /// </summary>
            public class UserAccount : ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="userid"></param>
                /// <param name="password"></param>
                public UserAccount(string userid, string password)
                {
                    if (userid == "" || password == "")
                        throw new Exception("User id and password cannot be empty");

                    UserID = userid;
                    Password = password;
                    PasswordExpiringDate = string.Empty;
                    IsNeverExpiring = false;
                    UserGroup = new List<UserRole>();
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="userid"></param>
                /// <param name="password"></param>
                /// <param name="expiringDate"></param>
                /// <param name="isExpiring"></param>
                /// <param name="roles"></param>
                public UserAccount(string userid, string password, string expiringDate, bool isExpiring, List<UserRole> roles)
                {
                    if (userid == "" || password == "")
                        throw new Exception("User id and password cannot be empty");
                    UserID = userid;
                    Password = password;
                    PasswordExpiringDate = expiringDate;
                    IsNeverExpiring = isExpiring;
                    UserGroup = roles.ToList();                    
                }

                #region Fields
                /// <summary>
                /// 使用者帳號
                /// </summary>
                [Required]
                public string UserID { get; set; }
                /// <summary>
                /// 使用者密碼
                /// </summary>
                [Required]
                public string Password { get; set; }
                /// <summary>
                /// 密碼有效日期
                /// </summary>
                public string PasswordExpiringDate { get; set; }
                /// <summary>
                /// 密碼永遠不會過期
                /// </summary>
                public bool IsNeverExpiring { get; set; }
                /// <summary>
                /// 使用者包含的角色群組
                /// </summary>
                [Required]
                public List<UserRole> UserGroup { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 複製副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new UserAccount(UserID, Password, PasswordExpiringDate, IsNeverExpiring, UserGroup);
                }
                #endregion
            }
            #endregion

            #region OpFunctionName
            /// <summary>
            /// QC功能名稱
            /// </summary>
            public class OpFunctionName : ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="name"></param>
                /// <param name="description"></param>
                public OpFunctionName(string name, string description)
                {
                    if (name == "")
                        throw new Exception("QC name cannot be empty");
                    Name = name;
                    Description = description;
                }

                #region Fields
                /// <summary>
                /// 名稱
                /// </summary>
                [Required]
                public string Name { get; set; }
                /// <summary>
                /// 說明
                /// </summary>
                public string Description { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 複製副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new OpFunctionName(Name, Description);
                }
                #endregion
            }
            #endregion
        }
    }
    #endregion
}
