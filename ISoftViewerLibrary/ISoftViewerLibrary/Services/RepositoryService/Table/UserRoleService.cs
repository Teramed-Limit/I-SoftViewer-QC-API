using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class UserRoleService : CommonRepositoryService<UserRole>
    {
        public UserRoleService(PacsDBOperationService dbOperator)
            : base("RoleGroup", dbOperator)
        {
            PrimaryKey = "RoleName";
            RelatedTablePrimaryKey = "FunctionName";
        }

        public override bool Delete(string roleName)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = PrimaryKey, Value = roleName }
            };

            var result = false;
            var operateTableList = new List<string> { "FunctionRoleGroup", "RoleGroup" };
            var last = operateTableList.Last();
            foreach (var table in operateTableList)
            {
                var commit = table.Equals(last);
                result = DbOperator
                    .BuildNoneQueryTable(table, primaryKeys, new List<PairDatas>())
                    .Remove(commit);

                if(!result) break;
            }

            return result;
        }

        public IEnumerable<RoleFunction> GetRoleFunctionList(string roleName)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = PrimaryKey, Value = roleName }
            };

            return DbOperator
                .BuildQueryTable("RoleFunctionView", primaryKeys, new List<PairDatas>())
                .Query<RoleFunction>();
        }

        public bool AddQCFunction(string roleName, string qcFunction)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = PrimaryKey, Value = roleName },
                new() { Name = RelatedTablePrimaryKey, Value = qcFunction }
            };

            return DbOperator
                .BuildNoneQueryTable("FunctionRoleGroup", primaryKeys, new List<PairDatas>())
                .AddOrUpdate();
        }

        public bool DeleteQcFunction(string roleName, string qcFunction)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = PrimaryKey, Value = roleName },
                new() { Name = RelatedTablePrimaryKey, Value = qcFunction }
            };

            return DbOperator
                .BuildNoneQueryTable("FunctionRoleGroup", primaryKeys, new List<PairDatas>())
                .Remove();
        }

        public IEnumerable<QCFunction> GetRoleFunctionList(LoginUserData loginUser)
        {
            var functionList = new List<QCFunction>();
            foreach (var roleName in loginUser.RoleList.Split(","))
            {
                if (string.IsNullOrEmpty(roleName)) continue;
                var primaryKeys = new List<PairDatas>
                {
                    new() { Name = "RoleName", Value = roleName }
                };

                var roleFunctions = DbOperator
                    .BuildQueryTable("RoleFunctionView", primaryKeys, new List<PairDatas>())
                    .Query<RoleFunction>();

                foreach (var roleFunction in roleFunctions)
                {
                    if (functionList.Any(x => x.FunctionName == roleFunction.FunctionName)) continue;
                    functionList.Add(new QCFunction()
                    {
                        FunctionName = roleFunction.FunctionName,
                        Description = roleFunction.Description,
                        CorrespondElementId = roleFunction.CorrespondElementId,
                    });
                }
            }

            return functionList;
        }

        public IEnumerable<QCFunction> GetAllFunctionList()
        {
            return DbOperator
                .BuildQueryTable("QCFunction", new List<PairDatas>(), new List<PairDatas>())
                .Query<QCFunction>();
        }
    }
}