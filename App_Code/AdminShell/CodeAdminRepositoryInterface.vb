Imports System.Collections.Generic

Public Interface ICodeAdminRepository
    Function ResolveMajorCode() As String
    Function ListEditableClasses() As IList(Of CodeAdminClass)
    Function GetClass(codeClass As String) As CodeAdminClass
    Function ListValues(codeClass As String, search As String) As IList(Of CodeAdminValue)
    Function ListLookupCodeValues(codeClass As String, selectedValues As IList(Of String), excludeValue As String) As IList(Of CodeAdminFieldOption)
    Function ListLookupCodeClasses(selectedValue As String) As IList(Of CodeAdminFieldOption)
    Function ListOrgSubTypeColumns(orgSubTypeCode As String, includeFunctionFields As Boolean) As IList(Of CodeAdminFieldOption)
    Function ListFacPrefEmailFields() As IList(Of CodeAdminFieldOption)
    Function ListProducts(organizationId As String) As IList(Of CodeAdminFieldOption)
    Function GetValueById(codeValueId As Integer) As CodeAdminValue
    Function GetValueByClassAndValue(codeClass As String, codeValue As String) As CodeAdminValue
    Function ValuePairExists(codeClass As String, codeValue As String, excludeId As Integer?) As Boolean
    Function CreateValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue
    Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue
    Function CreateLicenseObjTypeValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue
    Function UpdateLicenseObjTypeValue(command As UpdateCodeValueCommand, majorCode As String) As CodeAdminValue
    Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue
    Function DeleteValue(codeValueId As Integer) As CodeAdminDeleteResult
    Function DeleteLicenseObjTypeValue(codeValueId As Integer, majorCode As String) As CodeAdminDeleteResult
    Sub SetStatus(codeClass As String, codeValue As String, status As String)
    Sub SetPosition(codeClass As String, codeValue As String, newPosition As Integer)
End Interface
