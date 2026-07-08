<%
' ---------------------------------------------------------------------------
' Shared helpers for the legacy RangeOps reports (Classic ASP / VBScript).
' ---------------------------------------------------------------------------

' Open an ADO connection to the shared RangeOps database.
Function OpenDb()
    Dim c
    Set c = Server.CreateObject("ADODB.Connection")
    c.Open DB_CONN
    Set OpenDb = c
End Function

Sub RenderHeader(pageTitle)
    Response.Write "<!DOCTYPE html>" & vbCrLf
    Response.Write "<html><head><meta charset=""utf-8"">" & vbCrLf
    Response.Write "<title>" & Server.HTMLEncode(pageTitle) & " &mdash; RangeOps (legacy)</title>" & vbCrLf
    Response.Write "<style>" & _
        "body{font-family:Verdana,Arial,sans-serif;font-size:12px;color:#222;margin:24px;}" & _
        "h2{margin:0 0 2px;}" & _
        ".sub{color:#777;margin:0 0 16px;}" & _
        "table{border-collapse:collapse;}" & _
        "th,td{border:1px solid #999;padding:4px 8px;text-align:left;}" & _
        "th{background:#e8e8e8;}" & _
        "a{color:#00539b;}" & _
        ".legacy-note{margin-top:20px;color:#777;font-style:italic;font-size:11px;}" & _
        "</style></head><body>" & vbCrLf
    Response.Write "<h2>RangeOps &mdash; " & Server.HTMLEncode(pageTitle) & "</h2>" & vbCrLf
    Response.Write "<p class=""sub"">Legacy Classic ASP report</p>" & vbCrLf
End Sub

Sub RenderFooter()
    Response.Write "<p class=""legacy-note"">This report is a legacy Classic ASP page, " & _
        "superseded by the RangeOps web dashboard (Python/Django). Kept as the " & _
        "system-of-record report during migration.</p>" & vbCrLf
    Response.Write "</body></html>"
End Sub
%>
