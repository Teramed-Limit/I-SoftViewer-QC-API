using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ISoftViewerLibrary.Models.DTOs
{
    public class LoginRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class LoginResult
    {
        [JsonPropertyName("username")] public string UserName { get; set; }

        [JsonPropertyName("functionList")] public List<QCFunction> FunctionList { get; set; }

        [JsonPropertyName("accessToken")] public string AccessToken { get; set; }

        [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; }
    }

    public class RefreshTokenRequest
    {
        [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; }
    }

    public class LoginUserData : JsonDatasetBase
    {
        [Required] public string UserID { get; set; }

        [Required] public string UserPassword { get; set; }

        public string DoctorCode { get; set; }

        public string DoctorCName { get; set; }

        public string DoctorEName { get; set; }

        public string IsSupervisor { get; set; }

        public string RoleList { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public string Title { get; set; }

        public string Qualification { get; set; }

        public string SignatureBase64 { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }

    public class LoginUserDataDto
    {
        public string UserID { get; set; }

        public string UserPassword { get; set; }

        public string DoctorCode { get; set; }

        public string DoctorCName { get; set; }

        public string DoctorEName { get; set; }

        public string IsSupervisor { get; set; }

        public List<string> RoleList { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public string Title { get; set; }

        public string Qualification { get; set; }

        public string SignatureBase64 { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}