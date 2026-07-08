<%
' ---------------------------------------------------------------------------
' Copy this file to config.asp (which is git-ignored) and set your connection
' string. Requires the PostgreSQL ODBC driver (psqlODBC) on the IIS host.
'
' These legacy reports point at the SAME RangeOps database the modern stack
' uses; by default, the local docker-compose Postgres.
' ---------------------------------------------------------------------------
Const DB_CONN = "Driver={PostgreSQL Unicode};Server=localhost;Port=5544;" & _
                "Database=rangeops;Uid=rangeops;Pwd=rangeops;"
%>
