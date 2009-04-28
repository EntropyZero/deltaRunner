if exists (Select * from sysdatabases where name = 'DeltaRunner_FullTest1')
BEGIN

	    	ALTER DATABASE [DeltaRunner_FullTest1] 
		    SET SINGLE_USER 
		    WITH ROLLBACK IMMEDIATE

		    DROP DATABASE [DeltaRunner_FullTest1] 
END;
go