<%@ Language="VBScript" %>
<% Option Explicit %>
<!-- #include file="config.asp" -->
<!-- #include file="lib.asp" -->
<%
' ===========================================================================
' mission-report.asp?id=<n>
' Per-mission test-run report (legacy Classic ASP). Parallels the modern
' Django mission-detail page. Uses a PARAMETERIZED ADO command -- user input
' is never concatenated into SQL.
' ===========================================================================
Const adInteger = 3
Const adParamInput = 1

Dim missionId, conn, cmd, mrs, rrs

missionId = Request.QueryString("id")
If Not IsNumeric(missionId) Then
    RenderHeader "Mission Report"
    Response.Write "<p>Invalid or missing mission id.</p>"
    Response.Write "<p><a href=""schedule-report.asp"">&larr; Back to schedule</a></p>"
    RenderFooter
    Response.End
End If

Set conn = OpenDb()

' --- mission header (parameterized) ---
Set cmd = Server.CreateObject("ADODB.Command")
cmd.ActiveConnection = conn
cmd.CommandText = "SELECT name, aircraft, status FROM missions WHERE id = ?"
cmd.Parameters.Append cmd.CreateParameter("@id", adInteger, adParamInput, , CLng(missionId))
Set mrs = cmd.Execute()

If mrs.EOF Then
    RenderHeader "Mission Report"
    Response.Write "<p>No mission #" & Server.HTMLEncode(missionId) & ".</p>"
    Response.Write "<p><a href=""schedule-report.asp"">&larr; Back to schedule</a></p>"
    mrs.Close : conn.Close
    RenderFooter
    Response.End
End If

RenderHeader mrs("name") & " (" & mrs("aircraft") & ") — " & mrs("status")
mrs.Close : Set mrs = Nothing
%>
<p><a href="schedule-report.asp">&larr; Back to schedule</a></p>
<table>
  <tr>
    <th>Test run</th><th>Status</th><th>Samples</th><th>Link dropouts</th><th>Notes</th>
  </tr>
<%
' --- test runs with telemetry rollups (parameterized) ---
Set cmd = Server.CreateObject("ADODB.Command")
cmd.ActiveConnection = conn
cmd.CommandText = _
    "SELECT r.name, r.status, r.notes, " & _
    "  (SELECT COUNT(*) FROM telemetry_samples t WHERE t.test_run_id = r.id) AS samples, " & _
    "  (SELECT COUNT(*) FROM telemetry_samples t WHERE t.test_run_id = r.id AND t.link_dropout) AS dropouts " & _
    "FROM test_runs r WHERE r.mission_id = ? ORDER BY r.id"
cmd.Parameters.Append cmd.CreateParameter("@mid", adInteger, adParamInput, , CLng(missionId))
Set rrs = cmd.Execute()

Do While Not rrs.EOF
%>
  <tr>
    <td><%= Server.HTMLEncode(rrs("name")) %></td>
    <td><%= Server.HTMLEncode(rrs("status")) %></td>
    <td><%= rrs("samples") %></td>
    <td><%= rrs("dropouts") %></td>
    <td><%= Server.HTMLEncode(rrs("notes") & "") %></td>
  </tr>
<%
    rrs.MoveNext
Loop

rrs.Close : Set rrs = Nothing
conn.Close : Set conn = Nothing
%>
</table>
<% RenderFooter %>
