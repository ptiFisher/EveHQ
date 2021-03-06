﻿'==============================================================================
'
' EveHQ - An Eve-Online™ character assistance application
' Copyright © 2005-2015  EveHQ Development Team
'
' This file is part of EveHQ.
'
' The source code for EveHQ is free and you may redistribute 
' it and/or modify it under the terms of the MIT License. 
'
' Refer to the NOTICES file in the root folder of EVEHQ source
' project for details of 3rd party components that are covered
' under their own, separate licenses.
'
' EveHQ is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
' license below for details.
'
' ------------------------------------------------------------------------------
'
' The MIT License (MIT)
'
' Copyright © 2005-2015  EveHQ Development Team
'
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in
' all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
' THE SOFTWARE.
'
' ==============================================================================

Imports EveHQ.Prism.Classes
Imports EveHQ.Core
Imports DevComponents.DotNetBar.Controls
Imports System.Windows.Forms
Imports System.Text

Namespace Controls

    Public Class PrismSelectionControl

        Dim _startup As Boolean = True
        ReadOnly _host As TextBoxDropDown
        Dim _cListType As PrismSelectionType
        Dim _cAllowMultipleSelections As Boolean = True
        Dim _cExtraData As String = ""
        Dim _updateAll As Boolean = False
        Public Event SelectionChanged()

        Public Sub New(ByVal selectionType As PrismSelectionType, ByVal allowMultiSelection As Boolean, ByVal hostingControl As TextBoxDropDown)

            ' This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Set the parameter data
            _host = hostingControl
            _cListType = selectionType
            _cAllowMultipleSelections = allowMultiSelection
            _cExtraData = ""
            btnAll.Enabled = allowMultiSelection
            _startup = False
        End Sub

        Public Sub New(ByVal selectionType As PrismSelectionType, ByVal allowMultiSelection As Boolean, ByVal hostingControl As TextBoxDropDown, ByVal additionalData As String)

            ' This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Set the parameter data
            _host = hostingControl
            _cListType = selectionType
            _cAllowMultipleSelections = allowMultiSelection
            _cExtraData = additionalData
            btnAll.Enabled = allowMultiSelection
            _startup = False
        End Sub

        Public Property ListType As PrismSelectionType
            Get
                Return _cListType
            End Get
            Set(ByVal value As PrismSelectionType)
                _cListType = value
            End Set
        End Property

        Public Property AllowMultipleSelections As Boolean
            Get
                Return _cAllowMultipleSelections
            End Get
            Set(ByVal value As Boolean)
                _cAllowMultipleSelections = value
                btnAll.Enabled = value
            End Set
        End Property

        Public Property ExtraData As String
            Get
                Return _cExtraData
            End Get
            Set(ByVal value As String)
                _cExtraData = value
            End Set
        End Property

        Private Sub btnClose_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnClose.Click
            ' Close the host control
            _host.CloseDropDown()
        End Sub

        Private Sub btnAll_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAll.Click
            _updateAll = True
            For Each owner As ListViewItem In lvwItems.Items
                owner.Checked = True
            Next
            _updateAll = False
            UpdateOwnerList()
        End Sub

        Private Sub btnClear_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnClear.Click
            _updateAll = True
            For Each owner As ListViewItem In lvwItems.Items
                owner.Checked = False
            Next
            _updateAll = False
            UpdateOwnerList()
        End Sub

        Private Sub lvwOwners_ItemChecked(ByVal sender As Object, ByVal e As ItemCheckedEventArgs) Handles lvwItems.ItemChecked
            If _startup = False Then
                If _cAllowMultipleSelections = False Then
                    If lvwItems.CheckedItems.Count > 1 Then
                        ' Clear all checked items - acts like radio buttons
                        For Each item As ListViewItem In lvwItems.CheckedItems
                            item.Checked = False
                        Next
                        e.Item.Checked = True
                    End If
                    UpdateOwnerList()
                Else
                    If _updateAll = False Then
                        UpdateOwnerList()
                    End If
                End If
            End If
        End Sub

        Public Sub UpdateOwnerList()
            If _host IsNot Nothing Then
                If _cAllowMultipleSelections = True And lvwItems.CheckedItems.Count = lvwItems.Items.Count Then
                    _host.Text = "<All>"
                Else
                    Dim ownerList As New StringBuilder
                    For Each item As ListViewItem In lvwItems.CheckedItems
                        ownerList.Append(", " & item.Text)
                    Next
                    If ownerList.Length > 2 Then
                        ownerList.Remove(0, 2)
                    End If
                    _host.Text = ownerList.ToString
                End If
                RaiseEvent SelectionChanged()
            End If
        End Sub

        Public Sub UpdateList()

            ' Prepare list
            Dim itemData As New DataSet
            lvwItems.BeginUpdate()
            lvwItems.Items.Clear()

            ' Do stuff depending on the type of data we want
            Select Case _cListType

                Case PrismSelectionType.AllOwners
                    For Each owner As PrismOwner In PlugInData.PrismOwners.Values
                        If lvwItems.Items.ContainsKey(owner.Name) = False Then
                            Dim newItem As New ListViewItem(owner.Name)
                            newItem.Name = owner.Name
                            lvwItems.Items.Add(newItem)
                        End If
                    Next
                    CheckDefaultCharacter()

                Case PrismSelectionType.AllRefTypes
                    For Each ref As String In PlugInData.RefTypes.Keys
                        Dim newItem As New ListViewItem(PlugInData.RefTypes(ref))
                        newItem.Name = ref
                        lvwItems.Items.Add(newItem)
                    Next

                Case PrismSelectionType.JournalOwnersAll
                    Const StrSQL As String = "SELECT DISTINCT charName from walletJournal;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                            newItem.Name = dr.Item(0).ToString.Trim
                            lvwItems.Items.Add(newItem)
                        Next
                    End If
                    CheckDefaultCharacter()

                Case PrismSelectionType.JournalOwnersChars
                    Const StrSQL As String = "SELECT DISTINCT charName from walletJournal;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            If PlugInData.CorpList.ContainsKey(dr.Item(0).ToString.Trim) = False Then
                                Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                                newItem.Name = dr.Item(0).ToString.Trim
                                lvwItems.Items.Add(newItem)
                            End If
                        Next
                    End If
                    CheckDefaultCharacter()

                Case PrismSelectionType.JournalOwnersCorps
                    Const StrSQL As String = "SELECT DISTINCT charName from walletJournal;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            If PlugInData.CorpList.ContainsKey(dr.Item(0).ToString.Trim) = True Then
                                Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                                newItem.Name = dr.Item(0).ToString.Trim
                                lvwItems.Items.Add(newItem)
                            End If
                        Next
                    End If

                Case PrismSelectionType.JournalRefTypes
                    Const StrSQL As String = "SELECT DISTINCT refTypeID from walletJournal;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            Dim newItem As New ListViewItem(PlugInData.RefTypes(dr.Item(0).ToString))
                            newItem.Name = dr.Item(0).ToString
                            lvwItems.Items.Add(newItem)
                        Next
                    End If

                Case PrismSelectionType.TransactionOwnersAll
                    Const StrSQL As String = "SELECT DISTINCT charName from walletTransactions;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                            newItem.Name = dr.Item(0).ToString.Trim
                            lvwItems.Items.Add(newItem)
                        Next
                    End If
                    CheckDefaultCharacter()

                Case PrismSelectionType.TransactionOwnersChars
                    Const StrSQL As String = "SELECT DISTINCT charName from walletTransactions;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            If PlugInData.CorpList.ContainsKey(dr.Item(0).ToString.Trim) = False Then
                                Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                                newItem.Name = dr.Item(0).ToString.Trim
                                lvwItems.Items.Add(newItem)
                            End If
                        Next
                    End If
                    CheckDefaultCharacter()

                Case PrismSelectionType.TransactionOwnersCorps
                    Const StrSQL As String = "SELECT DISTINCT charName from walletTransactions;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            If PlugInData.CorpList.ContainsKey(dr.Item(0).ToString.Trim) = True Then
                                Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                                newItem.Name = dr.Item(0).ToString.Trim
                                lvwItems.Items.Add(newItem)
                            End If
                        Next
                    End If

                Case PrismSelectionType.TransactionItems
                    Dim strSQL As String
                    If _cExtraData = "" Then
                        strSQL = "SELECT DISTINCT typeName from walletTransactions;"
                    Else
                        strSQL = "SELECT DISTINCT typeName from walletTransactions WHERE charName='" & _cExtraData.Replace("'", "''") & "';"
                    End If
                    itemData = CustomDataFunctions.GetCustomData(strSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                            newItem.Name = dr.Item(0).ToString.Trim
                            lvwItems.Items.Add(newItem)
                        Next
                    End If

                Case PrismSelectionType.InventionInstallers
                    Const StrSQL As String = "SELECT DISTINCT installerName from inventionResults;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                            newItem.Name = dr.Item(0).ToString.Trim
                            lvwItems.Items.Add(newItem)
                        Next
                    End If
                    CheckDefaultCharacter()

                Case PrismSelectionType.InventionItems
                    Const StrSQL As String = "SELECT DISTINCT typeName from inventionResults;"
                    itemData = CustomDataFunctions.GetCustomData(StrSQL)
                    If itemData IsNot Nothing Then
                        For Each dr As DataRow In itemData.Tables(0).Rows
                            Dim newItem As New ListViewItem(dr.Item(0).ToString.Trim)
                            newItem.Name = dr.Item(0).ToString.Trim
                            lvwItems.Items.Add(newItem)
                        Next
                    End If

            End Select

            ' Finalise list
            lvwItems.Sort()
            lvwItems.EndUpdate()

            ' Dispose of the dataset
            If itemData IsNot Nothing Then
                itemData.Dispose()
            End If

        End Sub

        Private Sub PrismSelectionControl_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
            _startup = True
            Call UpdateList()
            _startup = False
        End Sub

        Private Sub CheckDefaultCharacter()
            If PrismSettings.UserSettings.DefaultCharacter <> "" Then
                If lvwItems.Items.Item(PrismSettings.UserSettings.DefaultCharacter) IsNot Nothing Then
                    lvwItems.Items.Item(PrismSettings.UserSettings.DefaultCharacter).Checked = True
                End If
                UpdateOwnerList()
            End If
        End Sub
    End Class

    Public Enum PrismSelectionType
        AllOwners = 0
        AllRefTypes = 1
        JournalOwnersAll = 2
        JournalRefTypes = 3
        TransactionOwnersAll = 4
        TransactionItems = 5
        JournalOwnersChars = 6
        JournalOwnersCorps = 7
        TransactionOwnersChars = 8
        TransactionOwnersCorps = 9
        InventionInstallers = 10
        InventionItems = 11
    End Enum
End Namespace