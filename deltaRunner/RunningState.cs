using System.Collections;
using System.Data;
using System.IO;
using System.Text;

namespace EntropyZero.deltaRunner
{
    public class RunningState
    {
        private DirectoryInfo deltaFolder;
        private string dbName;
        private DeltaFile[] deltaFiles;
        private ArrayList queuedFiles;
        private bool bSchemaModified = false;
        private bool reRunDelta = false;
        private bool shouldDropAndRecreate = false;
        private StringBuilder sbPostDeltaFilenamesToDelete = new StringBuilder();
        private Hashtable categories = new Hashtable();
        private int categoryCounter = 0;
        private string latestVersion;
        private DeltaHashProvider hashProvider;
    	private bool _ignoreExceptions;
    	private IDbConnection _currentConnection;
    	private IDbTransaction _currentTransaction;

    	public RunningState(string deltaPath)
        {
            deltaFolder = new DirectoryInfo(deltaPath);
        }

        public DeltaHashProvider HashProvider
        {
            get { return hashProvider; }
            set { hashProvider = value; }
        }

        public string LatestVersion
        {
            get { return latestVersion; }
            set { latestVersion = value; }
        }

        public Hashtable Categories
        {
            get { return categories; }
            set { categories = value; }
        }

        public int CategoryCounter
        {
            get { return categoryCounter; }
            set { categoryCounter = value; }
        }

        public string DBName
        {
            get { return dbName; }
            set { dbName = value; }
        }

        public DirectoryInfo DeltaFolder
        {
            get { return deltaFolder; }
            set { deltaFolder = value; }
        }

        public DeltaFile[] DeltaFiles
        {
            get { return deltaFiles; }
            set { deltaFiles = value; }
        }

        public ArrayList QueuedFiles
        {
            get { return queuedFiles; }
            set { queuedFiles = value; }
        }

        public bool SchemaModified
        {
            get { return bSchemaModified; }
            set { bSchemaModified = value; }
        }

        public bool ReRunDelta
        {
            get { return reRunDelta; }
            set { reRunDelta = value; }
        }

        public bool ShouldDropAndRecreate
        {
            get { return shouldDropAndRecreate; }
            set { shouldDropAndRecreate = value; }
        }

        public StringBuilder PostDeltaFilenamesToDelete
        {
            get { return sbPostDeltaFilenamesToDelete; }
            set { sbPostDeltaFilenamesToDelete = value; }
        }

    	public bool IgnoreExceptions
    	{
    		get { return _ignoreExceptions; }
    		set { _ignoreExceptions = value; }
    	}

    	public IDbConnection CurrentConnection
    	{
    		get { return _currentConnection; }
    		set { _currentConnection = value; }
    	}

    	public IDbTransaction CurrentTransaction
    	{
    		get { return _currentTransaction; }
    		set { _currentTransaction = value; }
    	}
    }
}
