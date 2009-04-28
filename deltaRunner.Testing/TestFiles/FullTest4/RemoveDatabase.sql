if exists (Select * from sysdatabases where name = 'DeltaRunner_FullTest4')
BEGIN

	    	ALTER DATABASE [DeltaRunner_FullTest4] 
		    SET SINGLE_USER 
		    WITH ROLLBACK IMMEDIATE

		    DROP DATABASE [DeltaRunner_FullTest4] 
END;
go