param(
    [string]$UserName = "dhoffman"
)

$ErrorActionPreference = "Stop"

$clientConfigPath = Join-Path $PSScriptRoot "..\..\..\..\web.config"
[xml]$clientConfig = Get-Content -LiteralPath $clientConfigPath
$connectionNode = $clientConfig.configuration.connectionStrings.add |
    Where-Object { $_.name -eq "ConnectionStringB" } |
    Select-Object -First 1

if (-not $connectionNode -or [string]::IsNullOrWhiteSpace($connectionNode.connectionString)) {
    throw "ConnectionStringB is not configured."
}

$connection = New-Object System.Data.Odbc.OdbcConnection($connectionNode.connectionString)
$connection.Open()

try {
    $userCommand = $connection.CreateCommand()
    $userCommand.CommandText = "select member_id from member_login where user_name = ? and inactive = 'N' and (account_locked is null or account_locked <> 'Y')"
    [void]$userCommand.Parameters.Add("@user_name", [System.Data.Odbc.OdbcType]::VarChar, 255)
    $userCommand.Parameters[0].Value = $UserName
    $memberId = $userCommand.ExecuteScalar()

    if ($null -eq $memberId -or $memberId -is [DBNull]) {
        throw "The configured pilot user is missing, inactive, or locked."
    }

    $accessCommand = $connection.CreateCommand()
    $accessCommand.CommandText = @"
select count(*) from (
    select a.access_id
    from script s
    join access a on a.secure_id = s.script_id and a.secure_ty = 'SCRI'
    where lower(s.script_name) = lower(?) and s.inactive = 'N' and a.inactive = 'N'
      and ((a.user_ty = 'USER' and a.user_id = ?)
        or (a.user_ty = 'GROU' and exists (
            select 1
            from group_member gm
            join ``group`` g on g.group_id = gm.group_id
            where gm.member_id = ? and gm.group_id = a.user_id and g.inactive = 'N')))
    union
    select a.access_id
    from script s
    join section_script ss on ss.script_id = s.script_id
    join section sn on sn.section_id = ss.section_id
    join access a on a.secure_id = sn.section_id and a.secure_ty = 'SECT'
    where lower(s.script_name) = lower(?) and s.inactive = 'N' and sn.inactive = 'N' and a.inactive = 'N'
      and ((a.user_ty = 'USER' and a.user_id = ?)
        or (a.user_ty = 'GROU' and exists (
            select 1
            from group_member gm
            join ``group`` g on g.group_id = gm.group_id
            where gm.member_id = ? and gm.group_id = a.user_id and g.inactive = 'N')))
) pilot_access
"@

    $parameterValues = @(
        "/admin/admin/views.asp",
        [int]$memberId,
        [int]$memberId,
        "/admin/admin/views.asp",
        [int]$memberId,
        [int]$memberId
    )

    foreach ($value in $parameterValues) {
        $parameter = $accessCommand.Parameters.Add("@value", [System.Data.Odbc.OdbcType]::VarChar, 512)
        $parameter.Value = $value
    }

    $accessCount = [int]$accessCommand.ExecuteScalar()
    if ($accessCount -lt 1) {
        throw "The configured pilot user does not have the canonical Views ACL."
    }

    Write-Output "Pilot data smoke test passed."
}
finally {
    $connection.Dispose()
}
