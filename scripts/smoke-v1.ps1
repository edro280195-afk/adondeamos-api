param(
    [string]$BaseUrl = "http://localhost:5172",
    [string]$Password = "SmokeTest!2026",
    [switch]$SkipSwagger
)

$ErrorActionPreference = "Stop"

$results = [System.Collections.Generic.List[object]]::new()

function Add-Step {
    param(
        [string]$Name,
        [int]$Status,
        [string]$Result,
        [object]$Data = $null
    )

    $results.Add([pscustomobject]@{
        step = $Name
        status = $Status
        result = $Result
        data = $Data
    }) | Out-Null
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "ASSERT: $Message"
    }
}

function Invoke-ApiJson {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$Token = $null
    )

    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $params = @{
        Uri = "$BaseUrl$Path"
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
        TimeoutSec = 60
    }

    if ($null -ne $Body) {
        $params["ContentType"] = "application/json"
        $params["Body"] = ($Body | ConvertTo-Json -Depth 20)
    }

    try {
        $response = Invoke-WebRequest @params
        $content = [string]$response.Content
        $json = $null

        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $trimmed = $content.TrimStart()
            if ($trimmed.StartsWith("{") -or $trimmed.StartsWith("[")) {
                $json = $content | ConvertFrom-Json
            }
            else {
                $json = $content
            }
        }

        Add-Step -Name $Name -Status ([int]$response.StatusCode) -Result "OK"
        return [pscustomobject]@{
            Status = [int]$response.StatusCode
            Json = $json
            Raw = $content
        }
    }
    catch {
        $status = 0
        $detail = $_.Exception.Message

        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }

        if ($_.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($_.ErrorDetails.Message)) {
            $detail = $_.ErrorDetails.Message
        }

        Add-Step -Name $Name -Status $status -Result "FAIL" -Data @{ error = $detail }
        throw "Paso fallo: $Name ($status) $detail"
    }
}

$BaseUrl = $BaseUrl.TrimEnd("/")

$healthy = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $health = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 5
        if ([int]$health.StatusCode -eq 200) {
            $healthy = $true
            break
        }
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

Assert-True $healthy "El API no respondio /health en tiempo."
Add-Step -Name "GET /health" -Status 200 -Result "OK" -Data @{ body = "Healthy" }

if (-not $SkipSwagger) {
    $swagger = Invoke-ApiJson -Name "GET /swagger/v1/swagger.json" -Method GET -Path "/swagger/v1/swagger.json"
    $paths = $swagger.Json.paths.PSObject.Properties.Name
    $requiredPaths = @(
        "/auth/register",
        "/auth/login",
        "/auth/confirm-email",
        "/auth/resend-confirmation",
        "/me",
        "/groups",
        "/groups/{id}",
        "/groups/{groupId}/invitations",
        "/me/invitations",
        "/invitations/{id}/accept",
        "/invitations/{id}/reject",
        "/places/search",
        "/places/resolve",
        "/places",
        "/saves",
        "/saves/{id}",
        "/lists",
        "/lists/{id}",
        "/lists/{id}/items",
        "/lists/{id}/items/{saveId}",
        "/decisions",
        "/decisions/{id}/options",
        "/decisions/{id}/options/{optionId}/votes",
        "/decisions/{id}"
    )

    foreach ($path in $requiredPaths) {
        Assert-True ($paths -contains $path) "Swagger no contiene $path"
    }
}

$runId = (Get-Date -Format "yyyyMMddHHmmss") + "-" + ((New-Guid).ToString("N").Substring(0, 8))
# username: solo letras, números, puntos y guiones bajos; 3-50 chars. Se deriva del runId eliminando el guion.
$runSlug = $runId -replace "-", "_"
$ownerUsername = "owner_$runSlug"
$guestUsername = "guest_$runSlug"
$declinerUsername = "dec_$runSlug"
$ownerEmail = "smoke-owner-$runId@example.com"
$guestEmail = "smoke-guest-$runId@example.com"
$declinerEmail = "smoke-decliner-$runId@example.com"

$ownerReg = Invoke-ApiJson -Name "POST /auth/register owner" -Method POST -Path "/auth/register" -Body @{
    name = "Smoke Owner $runId"
    username = $ownerUsername
    email = $ownerEmail
    password = $Password
}
$guestReg = Invoke-ApiJson -Name "POST /auth/register guest" -Method POST -Path "/auth/register" -Body @{
    name = "Smoke Guest $runId"
    username = $guestUsername
    email = $guestEmail
    password = $Password
}
$declinerReg = Invoke-ApiJson -Name "POST /auth/register decliner" -Method POST -Path "/auth/register" -Body @{
    name = "Smoke Decliner $runId"
    username = $declinerUsername
    email = $declinerEmail
    password = $Password
}
Assert-True ($ownerReg.Status -eq 201) "Registro owner no regreso 201."
Assert-True ($guestReg.Status -eq 201) "Registro guest no regreso 201."
Assert-True ($declinerReg.Status -eq 201) "Registro decliner no regreso 201."

# Con AutoConfirmEmail=true el usuario llega confirmado de inmediato.
Assert-True ([bool]$ownerReg.Json.user.emailConfirmed) "Owner debe tener email_confirmed=true (AutoConfirmEmail)."
Assert-True ([bool]$guestReg.Json.user.emailConfirmed) "Guest debe tener email_confirmed=true (AutoConfirmEmail)."

# El endpoint resend-confirmation responde 200 siempre (anti-enumeracion).
$resendResp = Invoke-ApiJson -Name "POST /auth/resend-confirmation" -Method POST -Path "/auth/resend-confirmation" -Body @{ email = $ownerEmail }
Assert-True ($resendResp.Status -eq 200) "resend-confirmation no regreso 200."

$ownerId = [guid]$ownerReg.Json.user.id
$guestId = [guid]$guestReg.Json.user.id
$declinerId = [guid]$declinerReg.Json.user.id

$ownerLogin = Invoke-ApiJson -Name "POST /auth/login owner" -Method POST -Path "/auth/login" -Body @{
    username = $ownerUsername
    password = $Password
}
$guestLogin = Invoke-ApiJson -Name "POST /auth/login guest" -Method POST -Path "/auth/login" -Body @{
    username = $guestUsername
    password = $Password
}
$declinerLogin = Invoke-ApiJson -Name "POST /auth/login decliner" -Method POST -Path "/auth/login" -Body @{
    username = $declinerUsername
    password = $Password
}

$ownerToken = [string]$ownerLogin.Json.accessToken
$guestToken = [string]$guestLogin.Json.accessToken
$declinerToken = [string]$declinerLogin.Json.accessToken
Assert-True (-not [string]::IsNullOrWhiteSpace($ownerToken)) "Owner token vacio."
Assert-True (-not [string]::IsNullOrWhiteSpace($guestToken)) "Guest token vacio."
Assert-True (-not [string]::IsNullOrWhiteSpace($declinerToken)) "Decliner token vacio."

$ownerMe = Invoke-ApiJson -Name "GET /me owner" -Method GET -Path "/me" -Token $ownerToken
$guestMe = Invoke-ApiJson -Name "GET /me guest" -Method GET -Path "/me" -Token $guestToken
$declinerMe = Invoke-ApiJson -Name "GET /me decliner" -Method GET -Path "/me" -Token $declinerToken
Assert-True ([guid]$ownerMe.Json.id -eq $ownerId) "GET /me owner no coincide."
Assert-True ([guid]$guestMe.Json.id -eq $guestId) "GET /me guest no coincide."
Assert-True ([guid]$declinerMe.Json.id -eq $declinerId) "GET /me decliner no coincide."

$ownerUpdated = Invoke-ApiJson -Name "PATCH /me owner" -Method PATCH -Path "/me" -Token $ownerToken -Body @{
    name = "Smoke Owner Actualizado $runId"
}
Assert-True ([string]$ownerUpdated.Json.name -eq "Smoke Owner Actualizado $runId") "PATCH /me no actualizo el nombre."

$group = Invoke-ApiJson -Name "POST /groups" -Method POST -Path "/groups" -Token $ownerToken -Body @{
    name = "Smoke Group $runId"
}
Assert-True ($group.Status -eq 201) "Crear grupo no regreso 201."
$groupId = [guid]$group.Json.id
Assert-True ($group.Json.members.Count -eq 1) "Grupo recien creado debe tener solo owner."

$groupBefore = Invoke-ApiJson -Name "GET /groups/{id} before accept" -Method GET -Path "/groups/$groupId" -Token $ownerToken
Assert-True ($groupBefore.Json.members.Count -eq 1) "Antes de aceptar, el invitado no debe estar en members."

$invitation = Invoke-ApiJson -Name "POST /groups/{id}/invitations" -Method POST -Path "/groups/$groupId/invitations" -Token $ownerToken -Body @{
    email = $guestEmail
}
Assert-True ($invitation.Status -eq 201) "Invitacion no regreso 201."
$invitationId = [guid]$invitation.Json.id
Assert-True ([string]$invitation.Json.status -eq "pending") "Invitacion no quedo pending."
Assert-True ([guid]$invitation.Json.invitedUser -eq $guestId) "Invitacion no apunta al invitado correcto."

$guestInvitations = Invoke-ApiJson -Name "GET /me/invitations guest" -Method GET -Path "/me/invitations" -Token $guestToken
$foundPending = @($guestInvitations.Json | Where-Object { [guid]$_.id -eq $invitationId -and [string]$_.status -eq "pending" })
Assert-True ($foundPending.Count -eq 1) "El invitado no ve la invitacion pendiente."

$accepted = Invoke-ApiJson -Name "POST /invitations/{id}/accept" -Method POST -Path "/invitations/$invitationId/accept" -Token $guestToken
Assert-True ([string]$accepted.Json.status -eq "accepted") "La invitacion no quedo accepted."
Assert-True ($null -ne $accepted.Json.respondedAt) "La invitacion aceptada no trae respondedAt."

$guestInvitationsAfter = Invoke-ApiJson -Name "GET /me/invitations guest after accept" -Method GET -Path "/me/invitations" -Token $guestToken
$stillPending = @($guestInvitationsAfter.Json | Where-Object { [guid]$_.id -eq $invitationId })
Assert-True ($stillPending.Count -eq 0) "La invitacion aceptada sigue apareciendo como pendiente."

$groupAfterOwner = Invoke-ApiJson -Name "GET /groups/{id} owner after accept" -Method GET -Path "/groups/$groupId" -Token $ownerToken
$groupAfterGuest = Invoke-ApiJson -Name "GET /groups/{id} guest after accept" -Method GET -Path "/groups/$groupId" -Token $guestToken
Assert-True ($groupAfterOwner.Json.members.Count -eq 2) "Owner no ve 2 miembros tras aceptar."
Assert-True ($groupAfterGuest.Json.members.Count -eq 2) "Guest no ve 2 miembros tras aceptar."

$declineInvitation = Invoke-ApiJson -Name "POST /groups/{id}/invitations decliner" -Method POST -Path "/groups/$groupId/invitations" -Token $ownerToken -Body @{
    userId = $declinerId
}
$declineInvitationId = [guid]$declineInvitation.Json.id
Assert-True ([string]$declineInvitation.Json.status -eq "pending") "Invitacion a decliner no quedo pending."

$declinerInvitations = Invoke-ApiJson -Name "GET /me/invitations decliner" -Method GET -Path "/me/invitations" -Token $declinerToken
Assert-True (@($declinerInvitations.Json | Where-Object { [guid]$_.id -eq $declineInvitationId }).Count -eq 1) "Decliner no ve su invitacion pendiente."

$rejected = Invoke-ApiJson -Name "POST /invitations/{id}/reject" -Method POST -Path "/invitations/$declineInvitationId/reject" -Token $declinerToken
Assert-True ([string]$rejected.Json.status -eq "rejected") "La invitacion no quedo rejected."
Assert-True ($null -ne $rejected.Json.respondedAt) "La invitacion rechazada no trae respondedAt."

$declinerInvitationsAfter = Invoke-ApiJson -Name "GET /me/invitations decliner after reject" -Method GET -Path "/me/invitations" -Token $declinerToken
Assert-True (@($declinerInvitationsAfter.Json | Where-Object { [guid]$_.id -eq $declineInvitationId }).Count -eq 0) "La invitacion rechazada sigue apareciendo como pendiente."

$groupAfterReject = Invoke-ApiJson -Name "GET /groups/{id} owner after reject" -Method GET -Path "/groups/$groupId" -Token $ownerToken
Assert-True ($groupAfterReject.Json.members.Count -eq 2) "Rechazar invitacion no debe agregar miembro."

$place = Invoke-ApiJson -Name "POST /places own" -Method POST -Path "/places" -Token $ownerToken -Body @{
    name = "Smoke Lugar $runId"
    latitude = 27.476
    longitude = -99.516
    city = "Nuevo Laredo"
}
Assert-True ($place.Status -eq 201) "Crear lugar propio no regreso 201."
$placeId = [guid]$place.Json.id
Assert-True ([string]$place.Json.origin -eq "own") "El lugar no quedo origin=own."

$ownerSave = Invoke-ApiJson -Name "POST /saves owner" -Method POST -Path "/saves" -Token $ownerToken -Body @{
    placeId = $placeId
    sourceNetwork = "manual"
    sourceUrl = "https://example.com/smoke-owner"
    note = "Smoke owner save $runId"
    visibility = "group"
}
$guestSave = Invoke-ApiJson -Name "POST /saves guest" -Method POST -Path "/saves" -Token $guestToken -Body @{
    placeId = $placeId
    sourceNetwork = "manual"
    sourceUrl = "https://example.com/smoke-guest"
    note = "Smoke guest save $runId"
    visibility = "group"
}
Assert-True ([string]$ownerSave.Json.status -eq "pending") "Owner save no quedo pending."
Assert-True ([string]$guestSave.Json.status -eq "pending") "Guest save no quedo pending."

$ownerSaves = Invoke-ApiJson -Name "GET /saves owner pending" -Method GET -Path "/saves?status=pending" -Token $ownerToken
$guestSaves = Invoke-ApiJson -Name "GET /saves guest pending" -Method GET -Path "/saves?status=pending" -Token $guestToken
Assert-True (@($ownerSaves.Json | Where-Object { [guid]$_.id -eq [guid]$ownerSave.Json.id }).Count -eq 1) "Owner no ve su guardado pendiente."
Assert-True (@($guestSaves.Json | Where-Object { [guid]$_.id -eq [guid]$guestSave.Json.id }).Count -eq 1) "Guest no ve su guardado pendiente."

$personalList = Invoke-ApiJson -Name "POST /lists personal" -Method POST -Path "/lists" -Token $ownerToken -Body @{
    name = "Smoke Lista Personal $runId"
}
$personalListId = [guid]$personalList.Json.id
$personalItem = Invoke-ApiJson -Name "POST /lists/{id}/items personal" -Method POST -Path "/lists/$personalListId/items" -Token $ownerToken -Body @{
    saveId = [guid]$ownerSave.Json.id
}
Assert-True ($personalItem.Status -eq 201) "Agregar item a lista personal no regreso 201."
$personalDetail = Invoke-ApiJson -Name "GET /lists/{id} personal" -Method GET -Path "/lists/$personalListId" -Token $ownerToken
Assert-True ($personalDetail.Json.items.Count -eq 1) "Lista personal no trae 1 item."

$groupList = Invoke-ApiJson -Name "POST /lists group" -Method POST -Path "/lists" -Token $ownerToken -Body @{
    name = "Smoke Lista Grupo $runId"
    groupId = $groupId
    visibility = "group"
}
$groupListId = [guid]$groupList.Json.id
$groupItem = Invoke-ApiJson -Name "POST /lists/{id}/items group guest" -Method POST -Path "/lists/$groupListId/items" -Token $guestToken -Body @{
    saveId = [guid]$guestSave.Json.id
}
Assert-True ($groupItem.Status -eq 201) "Guest no pudo agregar su save a lista de grupo."
$groupListOwnerView = Invoke-ApiJson -Name "GET /lists/{id} group owner" -Method GET -Path "/lists/$groupListId" -Token $ownerToken
$groupListGuestView = Invoke-ApiJson -Name "GET /lists/{id} group guest" -Method GET -Path "/lists/$groupListId" -Token $guestToken
Assert-True ($groupListOwnerView.Json.items.Count -eq 1) "Owner no ve item en lista de grupo."
Assert-True ($groupListGuestView.Json.items.Count -eq 1) "Guest no ve item en lista de grupo."

Invoke-ApiJson -Name "DELETE /lists/{id}/items/{saveId} group" -Method DELETE -Path "/lists/$groupListId/items/$([guid]$guestSave.Json.id)" -Token $guestToken | Out-Null

$decision = Invoke-ApiJson -Name "POST /decisions group" -Method POST -Path "/decisions" -Token $ownerToken -Body @{
    groupId = $groupId
    context = "Smoke match $runId"
}
$decisionId = [guid]$decision.Json.id
Assert-True ($decision.Json.participants.Count -eq 2) "La decision grupal no trae 2 participantes."

$options = Invoke-ApiJson -Name "POST /decisions/{id}/options autofill" -Method POST -Path "/decisions/$decisionId/options" -Token $ownerToken -Body @{
    autoFillFromSaves = $true
}
Assert-True ($options.Json.options.Count -ge 1) "No se agregaron opciones desde guardados pendientes."
$targetOption = @($options.Json.options | Where-Object { [guid]$_.place.id -eq $placeId }) | Select-Object -First 1
Assert-True ($null -ne $targetOption) "La opcion del lugar creado no aparecio en la decision."
$optionId = [guid]$targetOption.id

$voteOwner = Invoke-ApiJson -Name "POST /decisions/{id}/options/{optionId}/votes owner yes" -Method POST -Path "/decisions/$decisionId/options/$optionId/votes" -Token $ownerToken -Body @{
    isYes = $true
}
$afterOwnerOption = @($voteOwner.Json.options | Where-Object { [guid]$_.id -eq $optionId }) | Select-Object -First 1
Assert-True ($afterOwnerOption.votes.Count -eq 1) "Despues del primer voto debe haber 1 voto."
Assert-True (-not [bool]$afterOwnerOption.isMatch) "No debe haber match con solo 1 de 2 votos."

$voteGuest = Invoke-ApiJson -Name "POST /decisions/{id}/options/{optionId}/votes guest yes" -Method POST -Path "/decisions/$decisionId/options/$optionId/votes" -Token $guestToken -Body @{
    isYes = $true
}
$afterGuestOption = @($voteGuest.Json.options | Where-Object { [guid]$_.id -eq $optionId }) | Select-Object -First 1
Assert-True ($afterGuestOption.votes.Count -eq 2) "Despues del segundo voto debe haber 2 votos."
Assert-True ([bool]$afterGuestOption.isMatch) "Debe haber match cuando ambos votan si."
Assert-True (@($voteGuest.Json.matchedPlaceIds | Where-Object { [guid]$_ -eq $placeId }).Count -eq 1) "matchedPlaceIds no contiene el lugar esperado."

$decisionFinalOwner = Invoke-ApiJson -Name "GET /decisions/{id} owner final" -Method GET -Path "/decisions/$decisionId" -Token $ownerToken
$decisionFinalGuest = Invoke-ApiJson -Name "GET /decisions/{id} guest final" -Method GET -Path "/decisions/$decisionId" -Token $guestToken
Assert-True (@($decisionFinalOwner.Json.matchedPlaceIds | Where-Object { [guid]$_ -eq $placeId }).Count -eq 1) "Owner final no ve match persistido."
Assert-True (@($decisionFinalGuest.Json.matchedPlaceIds | Where-Object { [guid]$_ -eq $placeId }).Count -eq 1) "Guest final no ve match persistido."

$visited = Invoke-ApiJson -Name "PATCH /saves/{id} visited" -Method PATCH -Path "/saves/$([guid]$ownerSave.Json.id)" -Token $ownerToken -Body @{
    visited = $true
}
Assert-True ([string]$visited.Json.status -eq "visited") "PATCH /saves no marco visited."
Assert-True ($null -ne $visited.Json.visitedAt) "PATCH /saves visited no trae visitedAt."

Invoke-ApiJson -Name "DELETE /lists/{id}/items/{saveId} personal" -Method DELETE -Path "/lists/$personalListId/items/$([guid]$ownerSave.Json.id)" -Token $ownerToken | Out-Null
Invoke-ApiJson -Name "DELETE /saves/{id} guest" -Method DELETE -Path "/saves/$([guid]$guestSave.Json.id)" -Token $guestToken | Out-Null

$summary = [pscustomobject]@{
    runId = $runId
    baseUrl = $BaseUrl
    owner = @{ id = $ownerId; username = $ownerUsername; email = $ownerEmail }
    guest = @{ id = $guestId; username = $guestUsername; email = $guestEmail }
    decliner = @{ id = $declinerId; username = $declinerUsername; email = $declinerEmail }
    group = @{ id = $groupId; membersAfterAccept = $groupAfterOwner.Json.members.Count }
    invitation = @{
        id = $invitationId
        statusAfterAccept = [string]$accepted.Json.status
        rejectedInvitationId = $declineInvitationId
        statusAfterReject = [string]$rejected.Json.status
    }
    place = @{ id = $placeId; name = $place.Json.name; origin = [string]$place.Json.origin }
    saves = @{
        ownerSaveId = [guid]$ownerSave.Json.id
        guestSaveId = [guid]$guestSave.Json.id
        ownerSaveStatusAfterPatch = [string]$visited.Json.status
    }
    lists = @{
        personalListId = $personalListId
        groupListId = $groupListId
    }
    decision = @{
        id = $decisionId
        optionId = $optionId
        participants = $decisionFinalOwner.Json.participants.Count
        votes = $afterGuestOption.votes.Count
        matchedPlaceIds = @($decisionFinalOwner.Json.matchedPlaceIds)
    }
    steps = $results
}

$summary | ConvertTo-Json -Depth 20
