using System;
using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class UserAccountService : CommonRepositoryService<LoginUserData>
    {
        public UserAccountService(PacsDBOperationService dbOperator)
            : base("LoginUserData", dbOperator)
        {
            PrimaryKey = "UserID";
        }

        public LoginUserData GetUserData(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            // DB user
            var primaryKeys = new List<PairDatas> { new() { Name = "UserID", Value = userName }, };
            var loginUserList = DbOperator
                .BuildQueryTable("LoginUserData", primaryKeys, new List<PairDatas>())
                .Query<LoginUserData>();

            return !loginUserList.Any() ? null : loginUserList.First();
        }

        public LoginUserData IsValidUserCredentials(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // DB user pwd validation
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "UserID", Value = userName },
                new() { Name = "UserPassword", Value = password }
            };

            var loginUserList = DbOperator
                .BuildQueryTable("LoginUserData", primaryKeys, new List<PairDatas>())
                .Query<LoginUserData>();

            if (!loginUserList.Any())
                return null;

            return loginUserList.First();
        }

        public LoginUserData IsAnExistingUser(string userName)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "UserID", Value = userName },
            };

            var loginUserList = DbOperator
                .BuildQueryTable("LoginUserData", primaryKeys, new List<PairDatas>())
                .Query<LoginUserData>();

            if (!loginUserList.Any())
                return null;

            return loginUserList.First();
        }

        public string GenerateRefreshToken(string userId)
        {
            var refreshToken = Guid.NewGuid().ToString();

            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "UserID", Value = userId },
            };

            var updateValues = new List<PairDatas>
            {
                new()
                {
                    Name = "RefreshToken",
                    Value = refreshToken
                },
                new()
                {
                    Name = "RefreshTokenExpiryTime",
                    Value = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd HH:mm:ss")
                }
            };

            DbOperator
                .BuildNoneQueryTable("LoginUserData", primaryKeys, updateValues)
                .AddOrUpdate();

            return refreshToken;
        }

        public bool ClearRefreshToken(string userId)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "UserID", Value = userId },
            };

            var updateValues = new List<PairDatas>
            {
                new() { Name = "RefreshToken", Value = null },
                new() { Name = "RefreshTokenExpiryTime", Value = null }
            };

            return DbOperator
                .BuildNoneQueryTable("LoginUserData", primaryKeys, updateValues)
                .AddOrUpdate();
        }

        public bool ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "UserID", Value = userId },
                new() { Name = "RefreshToken", Value = refreshToken }
            };

            var loginUserList = DbOperator
                .BuildQueryTable("LoginUserData", primaryKeys, new List<PairDatas>())
                .Query<LoginUserData>()
                .ToList();

            return loginUserList.Any() && loginUserList.First().RefreshTokenExpiryTime > DateTime.Now;
        }
    }
}