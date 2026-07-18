Imports System.Collections.Generic

Public Interface ICodeAdminRepository
    Function ResolveMajorCode() As String
    Function ListEditableClasses() As IList(Of CodeAdminClass)
    Function GetClass(codeClass As String) As CodeAdminClass
    Function ListValues(codeClass As String, search As String) As IList(Of CodeAdminValue)
    Function GetValueById(codeValueId As Integer) As CodeAdminValue
    Function ValuePairExists(codeClass As String, codeValue As String, excludeId As Integer?) As Boolean
    Function CreateValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue
    Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue
    Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue
    Function DeleteValue(codeValueId As Integer) As CodeAdminDeleteResult
    Sub ActivateValue(codeClass As String, codeValue As String, majorCode As String)
    Sub DeactivateValue(codeClass As String, codeValue As String, majorCode As String)
    Sub SetPosition(codeClass As String, codeValue As String, newPosition As Integer)
End Interface
