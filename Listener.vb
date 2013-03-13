﻿'    WakeOnLAN - Wake On LAN
'    Copyright (C) 2004-2013 Aquila Technology, LLC. <webmaster@aquilatech.com>
'
'    This file is part of WakeOnLAN.
'
'    WakeOnLAN is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    WakeOnLAN is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with WakeOnLAN.  If not, see <http://www.gnu.org/licenses/>.

Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Public Class Listener
    Private gso As New StateObject
    Private Delegate Sub HitDelegate(ByVal ip As String)
    Private showHit As New HitDelegate(AddressOf hit)
    Private hitShown As Boolean = False

    Private Sub Listener_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        ListView1.Items.Clear()
        ReceiveMessages()
    End Sub

    Private Sub Listener_FormClosing(sender As System.Object, e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        gso.Socket.Close()
    End Sub

    Private Sub ReceiveMessages()
        gso.EndPoint = New IPEndPoint(IPAddress.Any, 9)

        gso.Socket = New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        gso.Socket.EnableBroadcast = True
        gso.Socket.Bind(gso.EndPoint)

        gso.Socket.BeginReceiveFrom(gso.Buffer, 0, gso.Buffer.Length, SocketFlags.None, gso.EndPoint, New AsyncCallback(AddressOf Async_Send_Receive), gso)
    End Sub

    Private Sub Async_Send_Receive(ByVal ar As IAsyncResult)
        Dim so As StateObject
        Dim i As Integer

        Try
            so = ar.AsyncState
            i = so.Socket.EndReceiveFrom(ar, so.EndPoint)
            Debug.WriteLine(Parse(so.Buffer, i))
            hit(Parse(so.Buffer, i))

            'Setup to receive the next packet
            so.Socket.BeginReceiveFrom(so.Buffer, 0, so.Buffer.Length, SocketFlags.None, so.EndPoint, New AsyncCallback(AddressOf Async_Send_Receive), so)

        Catch ex As Exception
            Debug.WriteLine(ex.Message)

        End Try
    End Sub

    Private Sub hit(MAC As String)
        If (ListView1.InvokeRequired) Then
            ListView1.Invoke(showHit, MAC)
        Else
            Dim li As New ListViewItem
            Dim n As String = ""

            li.Text = MAC
            li.ImageIndex = 0
            li.SubItems.Add(Now.ToShortTimeString)
            For Each m As Machine In Machines
                If (compareMAC(m.MAC, MAC) = 0) Then
                    n = m.Name
                End If
            Next
            li.SubItems.Add(n)

            ' always insert it at the top
            ListView1.Items.Insert(0, li)

            ' the next 2 lines are to work around a bug
            ' in the listview sorting
            ListView1.Alignment = ListViewAlignment.Default
            ListView1.Alignment = ListViewAlignment.Top

            If My.Settings.Sound Then
                My.Computer.Audio.Play(My.Resources.blip, AudioPlayMode.Background)
            End If
        End If
    End Sub

    Private Function compareMAC(mac1 As String, mac2 As String) As Int32
        Dim _mac1, _mac2 As String

        _mac1 = Replace(mac1, ":", "")
        _mac1 = Replace(_mac1, "-", "")

        _mac2 = Replace(mac2, ":", "")
        _mac2 = Replace(_mac2, "-", "")

        Return StrComp(_mac1, _mac2, CompareMethod.Text)

    End Function

    Private Function Parse(ByVal b As Byte(), l As Integer) As String
        Dim i As Int16
        Dim s As String = ""

        If l < 102 Then Return String.Empty

        For i = 0 To 5
            If b(i) <> 255 Then Return String.Empty
        Next

        For i = 1 To 15
            For j As Int16 = 0 To 5
                If b(6 + j) <> b(6 + j + (i * 6)) Then
                    Return String.Empty
                End If
            Next
        Next

        For i = 0 To 5
            If b(6 + i) < 16 Then
                s &= "0"
            End If

            s &= Hex(b(6 + i))
            If i < 5 Then s &= ":"
        Next

        Return s
    End Function

    Private Sub Button_clear_Click(sender As System.Object, e As System.EventArgs) Handles Button_clear.Click
        ListView1.Items.Clear()
    End Sub
End Class

Friend Class StateObject
    Public Buffer(127) As Byte
    Public Socket As Socket
    Public EndPoint As EndPoint
End Class