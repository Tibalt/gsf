'******************************************************************************************************
'  ProjectMacros.vb - Gbtc
'
'  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
'
'  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
'  the NOTICE file distributed with this work for additional information regarding copyright ownership.
'  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
'  not use this file except in compliance with the License. You may obtain a copy of the License at:
'
'      http://www.opensource.org/licenses/eclipse-1.0.php
' 
'  Unless agreed to in writing, the subject software distributed under the License is distributed on an 
'  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
'  License for the specific language governing permissions and limitations.
'
'  Code Modification History:
'  ----------------------------------------------------------------------------------------------------
'  02/08/2009 - James R. Carroll
'       Generated original version of source code.
'
'******************************************************************************************************

Imports System
Imports EnvDTE
Imports EnvDTE80
Imports EnvDTE90
Imports EnvDTE90a
Imports EnvDTE100
Imports System.Diagnostics
Imports System.DirectoryServices
Imports System.Security.Principal
Imports System.Text
Imports Microsoft.Win32

Public Module ProjectMacros

    Private m_userEntry As DirectoryEntry

    Public Sub InsertHeader()

        Dim activeDoc As Document = DTE.ActiveDocument
        Dim headerText = New StringBuilder()
        Dim commentToken As String

        ' Select proper in-line comment token
        If (activeDoc.Name.EndsWith(".vb")) Then
            commentToken = "'"
        Else
            commentToken = "//"
        End If

        With headerText
            .AppendLine(commentToken & "******************************************************************************************************")
            .AppendLine(commentToken & "  " & DTE.ActiveWindow.ProjectItem.Name & " - Gbtc")
            .AppendLine(commentToken & "")
            .AppendLine(commentToken & "  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.")
            .AppendLine(commentToken & "")
            .AppendLine(commentToken & "  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See")
            .AppendLine(commentToken & "  the NOTICE file distributed with this work for additional information regarding copyright ownership.")
            .AppendLine(commentToken & "  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the ""License""); you may")
            .AppendLine(commentToken & "  not use this file except in compliance with the License. You may obtain a copy of the License at:")
            .AppendLine(commentToken & "")
            .AppendLine(commentToken & "      http://www.opensource.org/licenses/eclipse-1.0.php")
            .AppendLine(commentToken & "")
            .AppendLine(commentToken & "  Unless agreed to in writing, the subject software distributed under the License is distributed on an")
            .AppendLine(commentToken & "  ""AS-IS"" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the")
            .AppendLine(commentToken & "  License for the specific language governing permissions and limitations.")
            .AppendLine(commentToken & "")
            .AppendLine(commentToken & "  Code Modification History:")
            .AppendLine(commentToken & "  ----------------------------------------------------------------------------------------------------")
            .AppendLine(commentToken & "  " & DateTime.Now.ToString("MM/dd/yyyy") & " - " & FullName)
            .AppendLine(commentToken & "       Generated original version of source code.")
            .AppendLine(commentToken & "")
            .AppendLine(commentToken & "******************************************************************************************************")
            .AppendLine()
        End With

        With activeDoc.Selection
            .StartOfDocument(False)
            .Insert(headerText.ToString(), vsInsertFlags.vsInsertFlagsCollapseToEnd)
        End With

    End Sub

    Public Sub FormatAll()

        Dim project As Project
        Dim projectObjects As Object()
        Dim window As Window
        Dim target As Object

        window = DTE.Windows.Item(Constants.vsWindowKindCommandWindow)
        projectObjects = DTE.ActiveSolutionProjects

        If projectObjects.Length = 0 Then
            Exit Sub
        End If

        project = DTE.ActiveSolutionProjects(0)

        If (DTE.ActiveWindow Is window) Then
            target = window.Object
        Else
            target = GetOutputWindowPane("List Project")
            target.Clear()
        End If

        RecurseProjectFolders(project.ProjectItems(), 0, target)

    End Sub

    Private Sub RecurseProjectFolders(ByVal projectItems As EnvDTE.ProjectItems, ByVal level As Integer, ByVal outputWinPane As Object)

        Dim projectItem As EnvDTE.ProjectItem

        For Each projectItem In projectItems
            ' Ignore item if it is not rooted in this collection (check for VC project model).
            If projectItem.Collection Is projectItems Then
                ' Execute formatting action
                PerformCodeItem(projectItem, level, outputWinPane)
                ' Recurse if this item has subitems ...
                Dim projectItems2 As EnvDTE.ProjectItems = projectItem.ProjectItems
                If projectItems2 IsNot Nothing Then RecurseProjectFolders(projectItems2, level + 1, outputWinPane)
            End If
        Next

    End Sub

    Private Sub PerformCodeItem(ByVal projectItem As EnvDTE.ProjectItem, ByVal level As Integer, ByVal outputWinPane As Object)

        Dim window As EnvDTE.Window
        Dim alreadyOpen As Boolean

        If projectItem.Name.EndsWith(".cs") Then
            alreadyOpen = projectItem.IsOpen(Constants.vsext_vk_Code)
            window = projectItem.Open(Constants.vsext_vk_Code)
            window.Activate()
            DTE.ExecuteCommand("Edit.FormatDocument")
            If Not alreadyOpen Then window.Close(vsSaveChanges.vsSaveChangesYes)
        End If

    End Sub

    Private Function GetOutputWindowPane(ByVal Name As String, Optional ByVal show As Boolean = True) As OutputWindowPane

        Dim window As Window
        Dim outputWindow As OutputWindow
        Dim outputWindowPane As OutputWindowPane

        window = DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput)
        If show Then window.Visible = True
        outputWindow = window.Object

        Try
            outputWindowPane = outputWindow.OutputWindowPanes.Item(Name)
        Catch e As System.Exception
            outputWindowPane = outputWindow.OutputWindowPanes.Add(Name)
        End Try

        outputWindowPane.Activate()

        Return outputWindowPane

    End Function

    Private ReadOnly Property FullName() As String
        Get
            ' If machine name and domain are the same, user is likely not logged into a domain so
            ' there's a good probablility that no active directory services will be available...
            If String.Compare(Environment.MachineName.Trim(), Environment.UserDomainName.Trim(), True) = 0 Then
                ' If not running on a domain, we use user name from Visual Studio registration
                Dim registeredName As String = Registry.GetValue("HKEY_USERS\.DEFAULT\Software\Microsoft\VisualStudio\10.0_Config\Registration", "UserName", "")

                If String.IsNullOrEmpty(registeredName) Then
                    Return Environment.UserName
                Else
                    Return registeredName
                End If
            Else
                ' Otherwise we get name defined in ActiveDirectory for logged in user
                If Not String.IsNullOrEmpty(FirstName) AndAlso Not String.IsNullOrEmpty(LastName) Then
                    If String.IsNullOrEmpty(MiddleInitial) Then
                        Return FirstName & " " & LastName
                    Else
                        Return FirstName & " " & MiddleInitial & ". " & LastName
                    End If
                Else
                    Return Environment.UserName
                End If
            End If
        End Get
    End Property

    Private ReadOnly Property UserEntry() As DirectoryEntry
        Get
            If m_userEntry Is Nothing Then
                Try
                    Dim entry As New DirectoryEntry()
                    With New DirectorySearcher(entry)
                        .Filter = "(SAMAccountName=" & Environment.UserName & ")"
                        m_userEntry = .FindOne().GetDirectoryEntry()
                    End With
                Catch
                    m_userEntry = Nothing
                    Throw
                End Try
            End If
            Return m_userEntry
        End Get
    End Property

    Private ReadOnly Property UserProperty(ByVal propertyName As System.String) As String
        Get
            Try
                Return UserEntry.Properties(propertyName)(0).ToString().Replace("  ", " ").Trim()
            Catch
                Return ""
            End Try
        End Get
    End Property

    Private ReadOnly Property FirstName() As String
        Get
            Return UserProperty("givenName")
        End Get
    End Property

    Private ReadOnly Property LastName() As String
        Get
            Return UserProperty("sn")
        End Get
    End Property

    Private ReadOnly Property MiddleInitial() As String
        Get
            Return UserProperty("initials")
        End Get
    End Property

    Private ReadOnly Property Email() As String
        Get
            Return UserProperty("mail")
        End Get
    End Property

    Private ReadOnly Property Telephone() As String
        Get
            Return UserProperty("telephoneNumber")
        End Get
    End Property

    Private ReadOnly Property Title() As String
        Get
            Return UserProperty("title")
        End Get
    End Property

    Private ReadOnly Property Company() As String
        Get
            Return UserProperty("company")
        End Get
    End Property

    Private ReadOnly Property Office() As String
        Get
            Return UserProperty("physicalDeliveryOfficeName")
        End Get
    End Property

    Private ReadOnly Property Department() As String
        Get
            Return UserProperty("department")
        End Get
    End Property

    Private ReadOnly Property City() As String
        Get
            Return UserProperty("l")
        End Get
    End Property

    Private ReadOnly Property Mailbox() As String
        Get
            Return UserProperty("streetAddress")
        End Get
    End Property

    Public Sub XmlCodeCommentRegion()

        Dim selection As EnvDTE.TextSelection
        Dim startPoint As EnvDTE.EditPoint
        Dim endPoint As TextPoint
        Dim commentStart As String

        selection = DTE.ActiveDocument.Selection()
        startPoint = selection.TopPoint.CreateEditPoint()
        endPoint = selection.BottomPoint
        commentStart = LineOrientedXmlCodeCommentStart()
        DTE.UndoContext.Open("Xml Code Comment Region")

        Try
            Do While (True)
                Dim line As Integer
                line = startPoint.Line
                startPoint.Insert(commentStart)
                startPoint.LineDown()
                startPoint.StartOfLine()
                If (line = endPoint.Line) Then Exit Do
            Loop
        Finally
            ' If an error occurred, then make sure that the undo context is cleaned up.
            ' Otherwise, the editor can be left in a perpetual undo context.
            DTE.UndoContext.Close()
        End Try

    End Sub

    Private Function LineOrientedXmlCodeCommentStart(Optional ByVal document As Document = Nothing) As String

        Dim extension As String

        If document Is Nothing Then document = DTE.ActiveDocument

        extension = document.Name

        If (extension.EndsWith(".cs") Or extension.EndsWith(".cpp") Or extension.EndsWith(".h") Or extension.EndsWith(".idl") Or extension.EndsWith(".jsl")) Then
            Return "/// "
        ElseIf (extension.EndsWith(".vb")) Then
            Return "''' "
        Else
            Throw New Exception("Unrecognized file type. You can add this file type by modifying the function LineOrientedXmlCodeCommentStart to include the extension of this file.")
        End If

    End Function

End Module
