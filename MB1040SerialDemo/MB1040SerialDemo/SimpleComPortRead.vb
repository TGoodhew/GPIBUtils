' This sample is licensed using the Microsoft Public License
' http://opensource.org/licenses/ms-pl
' Copyright 2014 Tony Goodhew
'
' If you're interested check out my Garage Door Project videos at
' https://www.youtube.com/playlist?list=PL0KF6L1yWbaYS6SosYc4qXFaEhNVftG8t


' This is the namespace that contains the serial ports
Imports System.IO.Ports

Public Class SimpleComPortRead

    Private ComPort As New SerialPort
    ' Useful URLs
    ' How to: Receive Strings From Serial Ports in Visual Basic http://msdn.microsoft.com/en-us/library/7ya7y41k.aspx
    ' How to: Invoke a Delegate Method (Visual Basic) http://msdn.microsoft.com/en-us/library/5t38cb9x.aspx
    ' SerialPort Class - http://msdn.microsoft.com/en-us/library/system.io.ports.serialport(v=vs.110).aspx

    Private Sub btnOpenCom3_Click(sender As Object, e As EventArgs) Handles btnOpenCom3.Click

        ' Firstly let's check to see if the port is open and if so close it
        If ComPort.IsOpen Then
            ComPort.Close()
        End If

        ' Setup the serial port - In my case its COM3
        With ComPort
            .BaudRate = 9600 ' These settings come from the sensor your reading
            .DataBits = 8
            .StopBits = StopBits.One
            .Parity = Parity.None
            .PortName = "COM3" ' I'm using COM3 but ideally you'd use the SerialPort.GetPortNames to get all the ports and let the user pick
            .NewLine = vbCr ' This is the VB constant that defines the end of line character that methods like ReadLine look for

        End With

        ' Let's add a handler to read the serial data - We could do a tight loop but using events means that we can do other things while waiting for data
        AddHandler ComPort.DataReceived, AddressOf DataReceivedHandler

        ' Now that we're setup let's open the com port - This will throw an exception if the comp port doesn't exist
        ' in real code you should wrap this in a Try...Catch to handle the exception case
        ComPort.Open()

    End Sub

    Private Sub DataReceivedHandler(sender As Object, e As SerialDataReceivedEventArgs)
        ' The event will pass in the serial port object as the sender so we can now use that but first we have to cast it correctly
        Dim dataPort As SerialPort = CType(sender, SerialPort)
        Dim receivedData As String

        ' Read the line of data as defined by the value in .NewLine above
        receivedData = dataPort.ReadLine()

        ' This is actually being executed on a background thread and as such doesn't have access to the UI thread.
        ' We need to invoke a handler to move to the right thread and we do that by using a delegate and Me.Invoke
        ' See the URLs at the top of the file for more information on this
        Dim del As New UpdateList(AddressOf ListUpdate)

        ' Invoke the handler and pass it the data
        Me.Invoke(del, receivedData)

    End Sub

    ' This is the delegate definition that we assigning the list update subroutine to
    Delegate Sub UpdateList(ByVal receivedData As String)

    ' This routine is going to be called correctly on the UI thread so we can party on the list box now
    Private Sub ListUpdate(ByVal receivedData As String)
        ListBox1.Items.Add(receivedData)
    End Sub

    ' Let's close the port as required
    Private Sub btnCloseCom3_Click(sender As Object, e As EventArgs) Handles btnCloseCom3.Click
        If ComPort.IsOpen Then
            ComPort.Close()
        End If
    End Sub
End Class
