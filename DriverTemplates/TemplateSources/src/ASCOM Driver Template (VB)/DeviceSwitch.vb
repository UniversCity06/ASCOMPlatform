﻿' All lines from line 1 to the device interface implementation region will be discarded by the project wizard when the template is used
' Required code must lie within the device implementation region
' The //ENDOFINSERTEDFILE tag must be the last but one line in this file

Imports ASCOM.DeviceInterface
Imports ASCOM
Imports ASCOM.Utilities

Class DeviceSwitch
    Implements ISwitchV2
    Private TL As New TraceLogger()

#Region "ISwitchV2 Implementation"
    Dim numSwitches As Short

    ''' <summary>
    ''' The number of switches managed by this driver
    ''' </summary>
    Public ReadOnly Property MaxSwitch As Short
        Get
            TL.LogMessage("MaxSwitch Get", numSwitches.ToString())
            Return numSwitches
        End Get
    End Property

    ''' <summary>
    ''' Return the name of switch n
    ''' </summary>
    ''' <param name="id">The switch number to return</param>
    ''' <returns>The name of the switch</returns>
    Public Function GetSwitchName(id As Short) As String
        Validate("GetSwitchName", id)
        TL.LogMessage("GetSwitchName", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("GetSwitchName")
    End Function

    ''' <summary>
    ''' Sets a switch name to a specified value
    ''' </summary>
    ''' <param name="id">The number of the switch whose name is to be set</param>
    ''' <param name="name">The name of the switch</param>
    Sub SetSwitchName(id As Short, name As String)
        Validate("SetSwitchName", id)
        TL.LogMessage("SetSwitchName", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("SetSwitchName")
    End Sub

#Region "boolean members"
    ''' <summary>
    ''' Return the state of switch n as a boolean
    ''' an analogue switch will return true if the value is closer to the maximum than the minimum, otherwise false
    ''' </summary>
    ''' <param name="id">The switch number to return</param>
    ''' <returns>True or false</returns>
    Function GetSwitch(id As Short) As Boolean
        Validate("GetSwitch", id)
        TL.LogMessage("GetSwitch", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("GetSwitch")
    End Function

    ''' <summary>
    ''' Sets a switch to the specified state, true or false.
    ''' If the switch cannot be set then throws a MethodNotImplementedException.
    ''' Setting an analogue switch to true will set it to its maximim value and
    ''' setting it to false will set it to its minimum value.
    ''' </summary>
    ''' <param name="ID">The number of the switch to set</param>
    ''' <param name="State">The required switch state</param>
    Sub SetSwitch(id As Short, state As Boolean)
        Validate("SetSwitch", id)
        TL.LogMessage("SetSwitch", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("SetSwitch")
    End Sub

#End Region

#Region "Analogue members"
    ''' <summary>
    ''' returns the maximum analogue value for this switch
    ''' boolean switches must return 1.0
    ''' </summary>
    ''' <param name="id"></param>
    ''' <returns></returns>
    Function MaxSwitchValue(id As Short) As Double
        Validate("MaxSwitchValue", id)
        TL.LogMessage("MaxSwitchValue", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("MaxSwitchValue")
    End Function

    ''' <summary>
    ''' returns the minimum analogue value for this switch
    ''' boolean switches must return 0.0
    ''' </summary>
    ''' <param name="id"></param>
    ''' <returns></returns>
    Function MinSwitchValue(id As Short) As Double
        Validate("MinSwitchValue", id)
        TL.LogMessage("MinSwitchValue", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("MinSwitchValue")
    End Function

    ''' <summary>
    ''' returns the step size that this switch supports. This gives the difference between
    ''' successive values of the switch.
    ''' The number of values is ((MaxSwitchValue - MinSwitchValue) / SwitchStep) + 1
    ''' boolean switches must return 1.0, giving two states.
    ''' </summary>
    ''' <param name="id"></param>
    ''' <returns></returns>
    Function SwitchStep(id As Short) As Double
        Validate("SwitchStep", id)
        TL.LogMessage("SwitchStep", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("SwitchStep")
    End Function

    ''' <summary>
    ''' returns the analogue switch value for switch id
    ''' boolean switches will return 1.0 or 0.0
    ''' </summary>
    ''' <param name="id"></param>
    ''' <returns></returns>
    Function GetSwitchValue(id As Short) As Double
        Validate("GetSwitchValue", id)
        TL.LogMessage("GetSwitchValue", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("GetSwitchValue")
    End Function

    ''' <summary>
    ''' set the analogue value for this switch.
    ''' If the switch cannot be set then throws a MethodNotImplementedException.
    ''' If the value is not between the maximum and minimum then throws an InvalidValueException
    ''' boolean switches will be set to true if the value is closer to the maximum than the minimum.
    ''' </summary>
    ''' <param name="id"></param>
    ''' <param name="value"></param>
    Sub SetSwitchValue(id As Short, value As Double)
        Validate("SetSwitchValue", id)
        If value < MinSwitchValue(id) Or value > MaxSwitchValue(id) Then
            Throw New InvalidValueException("", value.ToString(), String.Format("{0} to {1}", MinSwitchValue(id), MaxSwitchValue(id)))
        End If
        TL.LogMessage("SetSwitchValue", "Not Implemented")
        Throw New ASCOM.MethodNotImplementedException("SetSwitchValue")
    End Sub

#End Region
#End Region

    Private Sub Validate(message As String, id As Short)
        If (id < 0 Or id >= numSwitches) Then
            Throw New ASCOM.InvalidValueException(message, id.ToString(), String.Format("0 to {0}", numSwitches - 1))
        End If
    End Sub

    '//ENDOFINSERTEDFILE
End Class