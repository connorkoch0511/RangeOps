<%@ Language="VBScript" %>
<% Option Explicit %>
<!-- #include file="config.asp" -->
<!-- #include file="lib.asp" -->
<%
' ===========================================================================
' schedule-report.asp
' Mission Schedule report -- the legacy Classic ASP equivalent of the modern
' Django "schedule board". Reads the shared RangeOps database over ADO/ODBC.
' ===========================================================================
Dim conn, rs, sql

Set conn = OpenDb()

sql = "SELECT m.id, m.name, m.aircraft, m.status, " & _
      "       m.scheduled_start, m.scheduled_end, " & _
      "       (SELECT COUNT(*) FROM test_runs r WHERE r.mission_id = m.id) AS run_count " & _
      "FROM missions m " & _
      "ORDER BY m.scheduled_start DESC"

Set rs = conn.Execute(sql)

RenderHeader "Mission Schedule"
%>
<table>
  <tr>
    <th>Mission</th><th>Aircraft</th><th>Window</th><th>Runs</th><th>Status</th>
  </tr>
<%
Do While Not rs.EOF
%>
  <tr>
    <td><a href="mission-report.asp?id=<%= rs("id") %>"><%= Server.HTMLEncode(rs("name")) %></a></td>
    <td><%= Server.HTMLEncode(rs("aircraft")) %></td>
    <td><%= FormatDateTime(rs("scheduled_start"), 0) %></td>
    <td><%= rs("run_count") %></td>
    <td><%= Server.HTMLEncode(rs("status")) %></td>
  </tr>
<%
    rs.MoveNext
Loop

rs.Close : Set rs = Nothing
conn.Close : Set conn = Nothing
%>
</table>
<% RenderFooter %>
