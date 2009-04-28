if exists (Select * from sysdatabases where name = 'DeltaRunner_FullTest3')
BEGIN

	    	ALTER DATABASE [DeltaRunner_FullTest3] 
		    SET SINGLE_USER 
		    WITH ROLLBACK IMMEDIATE

		    DROP DATABASE [DeltaRunner_FullTest3] 
END;
go