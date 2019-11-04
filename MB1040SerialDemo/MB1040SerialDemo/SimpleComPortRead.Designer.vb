<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SimpleComPortRead
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.btnOpenCom3 = New System.Windows.Forms.Button()
        Me.btnCloseCom3 = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'ListBox1
        '
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.Location = New System.Drawing.Point(12, 12)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(320, 290)
        Me.ListBox1.TabIndex = 0
        '
        'btnOpenCom3
        '
        Me.btnOpenCom3.Location = New System.Drawing.Point(347, 33)
        Me.btnOpenCom3.Name = "btnOpenCom3"
        Me.btnOpenCom3.Size = New System.Drawing.Size(88, 48)
        Me.btnOpenCom3.TabIndex = 1
        Me.btnOpenCom3.Text = "Read COM3"
        Me.btnOpenCom3.UseVisualStyleBackColor = True
        '
        'btnCloseCom3
        '
        Me.btnCloseCom3.Location = New System.Drawing.Point(347, 94)
        Me.btnCloseCom3.Name = "btnCloseCom3"
        Me.btnCloseCom3.Size = New System.Drawing.Size(87, 49)
        Me.btnCloseCom3.TabIndex = 2
        Me.btnCloseCom3.Text = "Close COM3"
        Me.btnCloseCom3.UseVisualStyleBackColor = True
        '
        'SimpleComPortRead
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(444, 317)
        Me.Controls.Add(Me.btnCloseCom3)
        Me.Controls.Add(Me.btnOpenCom3)
        Me.Controls.Add(Me.ListBox1)
        Me.Name = "SimpleComPortRead"
        Me.Text = "Simple Com Port Read"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
    Friend WithEvents btnOpenCom3 As System.Windows.Forms.Button
    Friend WithEvents btnCloseCom3 As System.Windows.Forms.Button

End Class
