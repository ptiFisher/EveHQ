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
Imports EveHQ.EveData
Imports System.Threading.Tasks

Namespace BPCalc

    <Serializable()>
    Public Class InventionJob
        ' Invention specific items
        Private _inventedBpid As Integer
        Private _baseChance As Double
        Private _decryptorUsed As Decryptor
        Private _overrideBpcRuns As Boolean
        Private _bpcRuns As Integer
        Private _overrideEncSkill As Boolean
        Private _overrideDcSkill1 As Boolean
        Private _overrideDcSkill2 As Boolean
        Private _encryptionSkill As Integer
        Private _datacoreSkill1 As Integer
        Private _datacoreSkill2 As Integer
        Private _productionJob As Job

        Public Property InventedBpid As Integer
            Get
                Return _inventedBpid
            End Get
            Set(value As Integer)
                _inventedBpid = value
            End Set
        End Property

        Public Property BaseChance As Double
            Get
                Return _baseChance
            End Get
            Set(value As Double)
                _baseChance = value
            End Set
        End Property

        Public Property DecryptorUsed As Decryptor
            Get
                Return _decryptorUsed
            End Get
            Set(value As Decryptor)
                _decryptorUsed = value
            End Set
        End Property

        Public Property OverrideBpcRuns As Boolean
            Get
                Return _overrideBpcRuns
            End Get
            Set(value As Boolean)
                _overrideBpcRuns = value
            End Set
        End Property

        Public Property BpcRuns As Integer
            Get
                Return _bpcRuns
            End Get
            Set(value As Integer)
                _bpcRuns = value
            End Set
        End Property

        Public Property OverrideEncSkill As Boolean
            Get
                Return _overrideEncSkill
            End Get
            Set(value As Boolean)
                _overrideEncSkill = value
            End Set
        End Property

        Public Property OverrideDcSkill1 As Boolean
            Get
                Return _overrideDcSkill1
            End Get
            Set(value As Boolean)
                _overrideDcSkill1 = value
            End Set
        End Property

        Public Property OverrideDcSkill2 As Boolean
            Get
                Return _overrideDcSkill2
            End Get
            Set(value As Boolean)
                _overrideDcSkill2 = value
            End Set
        End Property

        Public Property EncryptionSkill As Integer
            Get
                Return _encryptionSkill
            End Get
            Set(value As Integer)
                _encryptionSkill = value
            End Set
        End Property

        Public Property DatacoreSkill1 As Integer
            Get
                Return _datacoreSkill1
            End Get
            Set(value As Integer)
                _datacoreSkill1 = value
            End Set
        End Property

        Public Property DatacoreSkill2 As Integer
            Get
                Return _datacoreSkill2
            End Get
            Set(value As Integer)
                _datacoreSkill2 = value
            End Set
        End Property

        Public Property ProductionJob As Job
            Get
                Return _productionJob
            End Get
            Set(value As Job)
                _productionJob = value
            End Set
        End Property

        ' Specific manufacturing info for the invention
        Public Function GetBaseBP() As EveData.Blueprint
            Return StaticData.Blueprints(StaticData.Blueprints(_inventedBpid).InventFrom(0))
        End Function

        Public Function CalculateInventionChance() As Double
            Dim decryptorMod As Double = 1
            If _decryptorUsed IsNot Nothing Then
                decryptorMod = _decryptorUsed.ProbMod
            End If
            'Return _baseChance * (1 + (0.01 * _encryptionSkill)) * (1 + ((_datacoreSkill1 + _datacoreSkill2) * (0.1 / (5 - _metaItemLevel)))) * decryptorMod
            Return _baseChance * 100 * (1 + (((_datacoreSkill1 + _datacoreSkill2) / 30) + (_encryptionSkill / 40))) * decryptorMod
        End Function

        Public Function CalculateInventionCost() As InventionCost
            'TODO: Refactor this so it is not a dupe of the blueprint Calc Invention Cost
            Dim invCost As New InventionCost
            Dim quantityTable As New Dictionary(Of Integer, Integer)
            ' Get base item BP for this invention
            Dim baseBp As EveData.Blueprint = GetBaseBP()

            ' Calculate Datacore costs
            For Each resource As EveData.BlueprintResource In baseBp.Resources(BlueprintActivity.Invention).Values
                ' Only include datacores
                If resource.TypeGroup = 333 Then
                    Dim idKey As Integer = resource.TypeId
                    If quantityTable.ContainsKey(idKey) = False Then
                        quantityTable.Add(idKey, resource.Quantity)
                    Else
                        quantityTable(idKey) = quantityTable(idKey) + resource.Quantity
                    End If
                End If
            Next

            ' Calculate Decryptor cost
            If _decryptorUsed IsNot Nothing Then
                If _decryptorUsed.ID <> 0 Then
                    If quantityTable.ContainsKey(_decryptorUsed.ID) = False Then
                        quantityTable.Add(_decryptorUsed.ID, 1)
                    Else
                        quantityTable(_decryptorUsed.ID) = quantityTable(_decryptorUsed.ID) + 1
                    End If
                End If
            End If

            ' Total the item costs

            Dim prices As Task(Of Dictionary(Of Integer, Double)) = DataFunctions.GetMarketPrices(quantityTable.Keys)
            prices.Wait()
            Dim itemCost As Dictionary(Of Integer, Double) = prices.Result

            invCost.DatacoreCost =
                itemCost.Keys.Where(
                    Function(key) baseBp.Resources(BlueprintActivity.Invention).Values.Any(Function(resource) resource.TypeId = key)).Sum(
                        Function(key) itemCost(key) * quantityTable(key))
            If _decryptorUsed IsNot Nothing Then
                invCost.DecryptorCost =
                    itemCost.Keys.Where(Function(key) key = _decryptorUsed.ID).Select(
                        Function(key) itemCost(key) * quantityTable(key)).Sum()
            End If

            ' Calculate lab cost
            invCost.LabCost = PrismSettings.UserSettings.LabInstallCost
            invCost.LabCost += Math.Round(PrismSettings.UserSettings.LabRunningCost * (baseBp.ResearchTechTime / 3600), 2,
                                          MidpointRounding.AwayFromZero)

            ' Calculate BPC cost
            invCost.BPCCost = CalculateBPCCost()
            invCost.TotalCost = invCost.DatacoreCost + invCost.DecryptorCost + invCost.LabCost + invCost.BPCCost

            Return invCost
        End Function

        Public Function CalculateInventedBPC() As OwnedBlueprint

            ' Get base item BP for this invention
            Dim baseBp As EveData.Blueprint = GetBaseBP()

            Dim ibp As OwnedBlueprint = OwnedBlueprint.CopyFromBlueprint(StaticData.Blueprints(_inventedBpid))

            Dim ime As Integer = 2
            Dim ipe As Integer = 4
            Dim irc As Integer
            Dim runMod As Integer = 0

            If _decryptorUsed IsNot Nothing Then
                If PlugInData.Decryptors.ContainsKey(_decryptorUsed.Name) Then
                    Dim useDecryptor As Decryptor = PlugInData.Decryptors(_decryptorUsed.Name)
                    ime += useDecryptor.MEMod
                    ipe += useDecryptor.PEMod
                    runMod = useDecryptor.RunMod
                End If
            End If
            irc = Math.Max(CInt(Math.Truncate((_bpcRuns / baseBp.MaxProductionLimit) * (ibp.MaxProductionLimit))), 1) + runMod

            ibp.MELevel = ime
            ibp.PELevel = ipe
            ibp.Runs = irc

            Return ibp
        End Function

        Public Function CalculateBPCCost() As Double
            Dim bpcCost As Double = 0
            Dim baseBp As EveData.Blueprint = GetBaseBP()
            If PrismSettings.UserSettings.BPCCosts.ContainsKey(baseBp.Id) Then
                Dim pricerange As Double = PrismSettings.UserSettings.BPCCosts(baseBp.Id).MaxRunCost - PrismSettings.UserSettings.BPCCosts(baseBp.Id).MinRunCost
                Dim runrange As Integer = baseBp.MaxProductionLimit - 1
                If runrange = 0 Then
                    bpcCost += PrismSettings.UserSettings.BPCCosts(baseBp.Id).MinRunCost
                Else
                    bpcCost += PrismSettings.UserSettings.BPCCosts(baseBp.Id).MinRunCost + Math.Round((pricerange / runrange) * (_bpcRuns - 1), 2, MidpointRounding.AwayFromZero)
                End If
            End If
            Return bpcCost
        End Function
    End Class
End Namespace