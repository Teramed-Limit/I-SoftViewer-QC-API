using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibrary.Models.ValueObjects;

namespace ISoftViewerLibrary.Services.RepositoryService
{
    public class PacsDBOperationService : DbOperationService<CustomizeTable>
    {
        private readonly EnvironmentConfiguration _config;

        public PacsDBOperationService(EnvironmentConfiguration config)
        {
            _config = config;
            GenerateUnitOfWork();
        }

        public void GenerateUnitOfWork()
        {
            DbTransactionUnitOfWork = new GeneralDatabaseOptUnitOfWork(
                "DbOperationServiceUnitOfWork",
                _config.DBUserID,
                _config.DBPassword,
                _config.DatabaseName,
                _config.ServerName);

            DbTransactionUnitOfWork.BeginTransaction();
            DbTransactionUnitOfWork.RegisterRepository(this);
        }
    }
}