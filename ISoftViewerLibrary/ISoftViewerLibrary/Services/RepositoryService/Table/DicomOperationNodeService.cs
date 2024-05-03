using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using System.Collections.Generic;
using System.Linq;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class DicomOperationNodeService : CommonRepositoryService<DicomOperationNodes>
    {
        private readonly EnvironmentConfiguration _config;

        public DicomOperationNodeService(PacsDBOperationService dbOperator, EnvironmentConfiguration config)
            : base("DicomOperationNodes", dbOperator)
        {
            PrimaryKey = "Name";
            _config = config;
        }

        public DicomOperationNodes GetTeramedCStoreNode()
        {
            var host = _config.DcmSendIP;
            var port = _config.DcmSendPort;
            var callingAE = _config.CallingAeTitle;
            var calledAE = _config.CalledAeTitle;
            
            return new DicomOperationNodes
            {
                RemoteAETitle = calledAE,
                AETitle = callingAE,
                IPAddress = host,
                Port = port
            };
        }

        public bool IsTeramedCStoreNode(DicomOperationNodes node)
        {
            var teramedNode = GetTeramedCStoreNode();

            return node.IPAddress == teramedNode.IPAddress && 
                   node.Port == teramedNode.Port && 
                   node.AETitle == teramedNode.AETitle && 
                   node.RemoteAETitle == teramedNode.RemoteAETitle;
        }

        public List<DicomOperationNodes> GetAllCStoreNode()
        {
            var list = GetEnableCStoreNode();
            list.Add(GetTeramedCStoreNode());
            return list;
        }

        public DicomOperationNodes GetOperationNode(string type, string name)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "OperationType", Value = type },
                new() { Name = "Name", Value = name },
                new() { Name = "Enable", Value = "1", Type = FieldType.ftInt }
            };

            return DbOperator
                .BuildQueryTable(TableName, primaryKeys, new List<PairDatas>())
                .Query<DicomOperationNodes>().ToList().First();
        }

        private List<DicomOperationNodes> GetEnableCStoreNode()
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = "OperationType", Value = "C-STORE" },
                new() { Name = "Enable", Value = "1", Type = FieldType.ftInt }
            };

            return DbOperator
                .BuildQueryTable(TableName, primaryKeys, new List<PairDatas>())
                .Query<DicomOperationNodes>().ToList();
        }
    }
}