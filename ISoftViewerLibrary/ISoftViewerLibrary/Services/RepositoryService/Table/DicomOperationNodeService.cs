using System;
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
        public DicomOperationNodeService(PacsDBOperationService dbOperator)
            : base("DicomOperationNodes", dbOperator)
        {
            PrimaryKey = "Name";
        }

        public DicomOperationNodes GetLocalCStoreNode()
        {
            var dicomOperationNodes = GetAll();
            var node = dicomOperationNodes.First(x => x.IsLocalStoreService == 1);

            if (node == null)
                throw new Exception("Can't find Teramed C-Store node");

            var host = node.IPAddress;
            var port = node.Port;
            var callingAE = node.AETitle;
            var calledAE = node.RemoteAETitle;

            return new DicomOperationNodes
            {
                RemoteAETitle = calledAE,
                AETitle = callingAE,
                IPAddress = host,
                Port = port
            };
        }

        public bool IsLocalCStoreNode(DicomOperationNodes node)
        {
            var localNode = GetLocalCStoreNode();

            return node.IPAddress == localNode.IPAddress &&
                   node.Port == localNode.Port &&
                   node.AETitle == localNode.AETitle &&
                   node.RemoteAETitle == localNode.RemoteAETitle;
        }

        public List<DicomOperationNodes> GetAllCStoreNode()
        {
            var list = GetEnableCStoreNode();
            list.Add(GetLocalCStoreNode());
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