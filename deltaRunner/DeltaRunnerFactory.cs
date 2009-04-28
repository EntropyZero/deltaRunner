namespace EntropyZero.deltaRunner
{
    public enum DeltaRunnerType
    {
        SqlServer
        , Sqlite
    }

    public static class DeltaRunnerFactory
    {
        public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath)
        {
            return (new SqlDeltaRunner(connectionString, deltaPath));
        }

        public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath, DeltaRunnerType deltaRunnerType)
        {
            switch (deltaRunnerType)
            {
                case DeltaRunnerType.Sqlite:
                    return (new SqliteDeltaRunner(connectionString, deltaPath));
                default:
                    return (new SqlDeltaRunner(connectionString, deltaPath));
            }
        }

        public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath, bool verbose)
        {
            return (new SqlDeltaRunner(connectionString, deltaPath, verbose));
        }

		public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath, bool verbose, bool shouldRunPostDeltasAllTheTime)
        {
			return (new SqlDeltaRunner(connectionString, deltaPath, verbose, shouldRunPostDeltasAllTheTime));
        }

        public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath, bool verbose, DeltaRunnerType deltaRunnerType)
        {
            switch (deltaRunnerType)
            {
                case DeltaRunnerType.Sqlite:
                    return (new SqliteDeltaRunner(connectionString, deltaPath, verbose));
                default:
                    return (new SqlDeltaRunner(connectionString, deltaPath, verbose));
            }
        }

        public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath, bool dropDbOnPreDeltaChange, string masterConnectionString, string dbName, bool verbose)
        {
            return (new SqlDeltaRunner(connectionString, deltaPath, dropDbOnPreDeltaChange, masterConnectionString, dbName, verbose));
        }

        public static DeltaRunnerBase CreateDeltaRunner(string connectionString, string deltaPath, bool dropDbOnPreDeltaChange, string masterConnectionString, string dbName, bool verbose, DeltaRunnerType deltaRunnerType)
        {
            switch(deltaRunnerType)
            {
                case DeltaRunnerType.Sqlite:
                    return (new SqliteDeltaRunner(connectionString, deltaPath, dropDbOnPreDeltaChange, masterConnectionString, dbName, verbose));
            
                default:
                    return (new SqlDeltaRunner(connectionString, deltaPath, dropDbOnPreDeltaChange, masterConnectionString, dbName, verbose));
            }
            
        }
    }
}