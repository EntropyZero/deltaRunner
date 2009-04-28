if exists (Select * from sysdatabases where name = 'DeltaRunner_FullTest2')
BEGIN

	    	ALTER DATABASE [DeltaRunner_FullTest2] 
		    SET SINGLE_USER 
		    WITH ROLLBACK IMMEDIATE

		    DROP DATABASE [DeltaRunner_FullTest2] 
END;
go

CREATE DATABASE [DeltaRunner_FullTest2]  
go