Imports System
Imports System.Collections.Generic

Public NotInheritable Class CodeAdminFieldMetadataRegistry
    Private Sub New()
    End Sub

    Public Shared Function Build(organizationId As String, codeClass As String) As CodeAdminDetailMetadata
        Dim fields = BuildDefaultFields()
        Dim normalizedClass = If(codeClass, String.Empty).Trim()
        Dim normalizedOrganization = If(organizationId, String.Empty).Trim()

        Select Case normalizedClass.ToUpperInvariant()
            Case "APP_RENEW_TYPE_CD"
                ConfigureField(fields, "codeValueLongDesc", "Path", "textarea", True)
                AddMinorCode(fields, "Record Type", "select", True, Nothing, "code-values:ORG_SUB_TY_CD")
            Case "RENEWAL_TYPE_CD", "APPLICATION_TYPE_CD"
                AddMinorCode(fields, "Record Type", "select", False, Nothing, "code-values:ORG_SUB_TY_CD")
            Case "WINDOW_SHADE_CD"
                AddMinorCode(fields, "Default State", "radio", False, New CodeAdminFieldOption() {
                    New CodeAdminFieldOption With {.Value = "block", .Label = "Open"},
                    New CodeAdminFieldOption With {.Value = "none", .Label = "Closed"}
                })
            Case "ZIP_CODES"
                If String.Equals(normalizedOrganization, "330", StringComparison.OrdinalIgnoreCase) Then
                    AddMinorCode(fields, "County", "select", False, Nothing, "code-values:SDBON_county_code")
                End If
            Case "ORG_SUB_TY_CD"
                AddMinorCode(fields, "Parent Type", "select", False, Nothing, "code-values:ORG_SUB_TY_CD")
                ConfigureOrgSubTypeFields(fields)
        End Select

        ConfigureOrganizationOverrides(fields, normalizedOrganization, normalizedClass)
        Return New CodeAdminDetailMetadata With {.Fields = fields}
    End Function

    Public Shared Function IsRequired(organizationId As String, codeClass As String, fieldKey As String) As Boolean
        Dim metadata = Build(organizationId, codeClass)
        Dim fieldIndex As Integer
        For fieldIndex = 0 To metadata.Fields.Count - 1
            If String.Equals(metadata.Fields(fieldIndex).Key, fieldKey, StringComparison.OrdinalIgnoreCase) Then
                Return metadata.Fields(fieldIndex).Required
            End If
        Next
        Return False
    End Function

    Private Shared Function BuildDefaultFields() As List(Of CodeAdminFieldMetadata)
        Dim fields As New List(Of CodeAdminFieldMetadata)()
        fields.Add(CreateField("codeValueDesc", "Description", "text", True, 10))
        fields.Add(CreateField("codeValueLongDesc", "Extended Description", "textarea", False, 20))
        fields.Add(CreateField("formDisplay", "Form Display", "text", False, 30))
        Dim optionIndex As Integer
        For optionIndex = 1 To 17
            fields.Add(CreateField("optionValue" & optionIndex.ToString(Globalization.CultureInfo.InvariantCulture), "Optional Value " & optionIndex.ToString(Globalization.CultureInfo.InvariantCulture), "text", False, 30 + optionIndex))
        Next
        Return fields
    End Function

    Private Shared Function CreateField(key As String, label As String, controlType As String, required As Boolean, order As Integer, Optional options As IList(Of CodeAdminFieldOption) = Nothing, Optional lookupSource As String = Nothing) As CodeAdminFieldMetadata
        Return New CodeAdminFieldMetadata With {
            .Key = key,
            .Label = label,
            .ControlType = controlType,
            .LookupSource = lookupSource,
            .Required = required,
            .Options = If(options, New List(Of CodeAdminFieldOption)()),
            .Section = "Details",
            .Order = order
        }
    End Function

    Private Shared Sub AddMinorCode(fields As List(Of CodeAdminFieldMetadata), label As String, controlType As String, required As Boolean, Optional options As IList(Of CodeAdminFieldOption) = Nothing, Optional lookupSource As String = Nothing)
        fields.Add(CreateField("minorCode", label, controlType, required, 25, options, lookupSource))
    End Sub

    Private Shared Sub ConfigureField(fields As List(Of CodeAdminFieldMetadata), key As String, label As String, controlType As String, required As Boolean, Optional options As IList(Of CodeAdminFieldOption) = Nothing, Optional lookupSource As String = Nothing)
        Dim fieldIndex As Integer
        For fieldIndex = 0 To fields.Count - 1
            If String.Equals(fields(fieldIndex).Key, key, StringComparison.OrdinalIgnoreCase) Then
                fields(fieldIndex).Label = label
                fields(fieldIndex).ControlType = controlType
                fields(fieldIndex).Required = required
                If options IsNot Nothing Then
                    fields(fieldIndex).Options = options
                End If
                fields(fieldIndex).LookupSource = lookupSource
                Return
            End If
        Next
    End Sub

    Private Shared Sub ConfigureOrgSubTypeFields(fields As List(Of CodeAdminFieldMetadata))
        ConfigureField(fields, "optionValue1", "Hide in Portal", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue2", "Identifier Field", "select", False, Nothing, "org-sub-type-columns")
        ConfigureField(fields, "optionValue3", "Identifier Field", "select", False, Nothing, "org-sub-type-columns")
        ConfigureField(fields, "optionValue4", "Identifier Field", "select", False, Nothing, "org-sub-type-columns")
        ConfigureField(fields, "optionValue5", "Parent/Grandparent Record Label", "text", False)
        ConfigureField(fields, "optionValue6", "Child/Grandchild Record Label", "text", False)
        ConfigureField(fields, "optionValue7", "Other Record Label", "text", False)
        ConfigureField(fields, "optionValue8", "Hide Contacts Tab", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue9", "Hide Resources Tab", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue10", "Hide Tasks Tab", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue11", "Hide Reminders Tab", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue12", "Hide Parent Tab", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue13", "Hide Child Tabs", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue14", "Default Sort Field", "select", False, Nothing, "org-sub-type-sort-columns")
        ConfigureField(fields, "optionValue15", "Default Sort Order", "radio", False, New CodeAdminFieldOption() {
            New CodeAdminFieldOption With {.Value = "ASC", .Label = "Ascending"},
            New CodeAdminFieldOption With {.Value = "DESC", .Label = "Descending"}
        })
        ConfigureField(fields, "optionValue16", "Hide Grandparent Tabs", "radio", False, YesNoOptions())
        ConfigureField(fields, "optionValue17", "Hide Grandchild Tabs", "radio", False, YesNoOptions())
    End Sub

    Private Shared Sub ConfigureOrganizationOverrides(fields As List(Of CodeAdminFieldMetadata), organizationId As String, codeClass As String)
        If organizationId = "5500" AndAlso String.Equals(codeClass, "LIC_TYPE", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Show Renew Button", "radio", False, YesNoOptions())
            ConfigureField(fields, "optionValue2", "Dashboard Renew Form", "select", False, Nothing, "code-values:APP_RENEW_TYPE_CD")
            ConfigureField(fields, "optionValue3", "Related Practioner License Type", "select", False, Nothing, "code-values:LIC_TYPE")
            ConfigureField(fields, "optionValue4", "Show CE Link", "radio", False, YesNoOptions())
        ElseIf organizationId = "1900" AndAlso String.Equals(codeClass, "CERT_LVL_CD", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue5", "Send to Awaiting Updates", "radio", False, YesNoOptions("Y", "N"))
        ElseIf organizationId = "1900" AndAlso String.Equals(codeClass, "FAC_TYPE", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Include in Registrants per Facility Type Report", "radio", False, YesNoOptions())
        ElseIf organizationId = "225" AndAlso String.Equals(codeClass, "Business_Type", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Parent Value", "select", False, Nothing, "code-values:BUSTYPE_MJR")
        ElseIf (organizationId = "1300" OrElse organizationId = "800" OrElse organizationId = "725" OrElse organizationId = "775" OrElse organizationId = "375") AndAlso String.Equals(codeClass, "APP_RENEW_TYPE_CD", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "ePay ID", "text", False)
        ElseIf organizationId = "825" AndAlso String.Equals(codeClass, "Warning", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Application/Renewal", "select", False, Nothing, "code-values:WarningForms")
        ElseIf organizationId = "825" AndAlso String.Equals(codeClass, "CredentialDefinitionIdnt", StringComparison.OrdinalIgnoreCase) Then
            ConfigureCredentialDefinitionFields(fields)
        ElseIf organizationId = "825" AndAlso String.Equals(codeClass, "APP_RENEW_TYPE_CD", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "PayPal Partner", "text", False)
            ConfigureField(fields, "optionValue2", "PayPal Vendor", "text", False)
            ConfigureField(fields, "optionValue3", "PayPal User", "text", False)
            ConfigureField(fields, "optionValue4", "PayPal Password", "text", False)
        ElseIf organizationId = "750" AndAlso String.Equals(codeClass, "APP_RENEW_TYPE_CD", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue8", "Passing Score for Tests", "text", False)
        ElseIf organizationId = "1050" AndAlso String.Equals(codeClass, "LicenseType", StringComparison.OrdinalIgnoreCase) Then
            ConfigureLicenseTypeFields(fields)
        ElseIf organizationId = "2500" AndAlso String.Equals(codeClass, "Shades", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Child Types to Show", "multiselect", False, Nothing, "code-values:ORG_SUB_TY_CD:exclude:Separators")
        ElseIf organizationId = "2500" AndAlso String.Equals(codeClass, "Type_of_Pipe", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Description Class", "select", False, Nothing, "code-classes")
        ElseIf organizationId = "1900" AndAlso String.Equals(codeClass, "FAC_PREF", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Preferred Email Field", "select", False, Nothing, "fac-pref-email-fields")
        ElseIf organizationId = "3900" AndAlso String.Equals(codeClass, "LicenseObjType", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "Window Shades to Show", "multiselect", False, Nothing, "code-values:WINDOW_SHADE_CD")
            ConfigureField(fields, "optionValue2", "Groups with Access", "multiselect", False, Nothing, "code-values:GROUP_TY_CD")
            ConfigureField(fields, "optionValue3", "License Group", "select", False, Nothing, "code-values:LicenseGroups")
        ElseIf organizationId = "3900" AndAlso String.Equals(codeClass, "APP_RENEW_TYPE_CD", StringComparison.OrdinalIgnoreCase) Then
            ConfigureField(fields, "optionValue1", "License Obj Types for Duplicate Checks", "multiselect", False, Nothing, "code-values:LicenseObjType")
        End If
    End Sub

    Private Shared Sub ConfigureCredentialDefinitionFields(fields As List(Of CodeAdminFieldMetadata))
        ConfigureField(fields, "optionValue1", "Renewal Product", "select", False, Nothing, "products")
        ConfigureField(fields, "optionValue2", "Late Renewal Product", "select", False, Nothing, "products")
        ConfigureField(fields, "optionValue3", "Credential Type", "select", False, Nothing, "code-values:CredentialTypeID")
        ConfigureField(fields, "optionValue4", "Credential Type Prefix", "text", False)
        ConfigureField(fields, "optionValue5", "Subcategory", "text", False)
        ConfigureField(fields, "optionValue6", "Application Product", "select", False, Nothing, "products")
        ConfigureField(fields, "optionValue7", "Verification Page Order", "text", False)
        ConfigureField(fields, "optionValue8", "Supervisor Type", "select", False, New CodeAdminFieldOption() {
            New CodeAdminFieldOption With {.Value = "1", .Label = "Supervisor"},
            New CodeAdminFieldOption With {.Value = "2", .Label = "Supervisee"}
        })
    End Sub

    Private Shared Sub ConfigureLicenseTypeFields(fields As List(Of CodeAdminFieldMetadata))
        ConfigureField(fields, "optionValue1", "License Card - licType", "text", False)
        ConfigureField(fields, "optionValue2", "License Card - license", "text", False)
        ConfigureField(fields, "optionValue3", "License Card - cardType", "text", False)
        ConfigureField(fields, "optionValue4", "License Card - licTxt", "text", False)
        ConfigureField(fields, "optionValue5", "License Card - type", "text", False)
        ConfigureField(fields, "optionValue6", "License Card - cardTxt", "text", False)
    End Sub

    Private Shared Function YesNoOptions(Optional yesValue As String = "1", Optional noValue As String = "0") As IList(Of CodeAdminFieldOption)
        Return New CodeAdminFieldOption() {
            New CodeAdminFieldOption With {.Value = yesValue, .Label = "Yes"},
            New CodeAdminFieldOption With {.Value = noValue, .Label = "No"}
        }
    End Function
End Class