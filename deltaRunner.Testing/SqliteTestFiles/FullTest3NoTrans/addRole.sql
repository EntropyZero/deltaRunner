use master
go
IF NOT Exists (select * from syslogins where name = 'ASP_NET_WEB')
EXEC sp_addlogin 'ASP_NET_WEB', 'hi', 'master';
GO

use DeltaRunner_FullTest3
go
Exec sp_adduser 'ASP_NET_WEB', 'ASP_NET_WEB', 'db_owner'
go

if @@trancount > 0
Begin
	RAISERROR ('DIE DIE.', -- Message text.
               18, -- Severity.
               1 -- State.
               );
               


End
GO

SP_ADDROLEMEMBER 'db_accessadmin', 'ASP_NET_WEB'
GO