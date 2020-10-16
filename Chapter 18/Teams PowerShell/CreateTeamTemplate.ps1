#Parameters
$name = "Team Name";
$description = "Team description";
$mailNickName = "NameBefore@";
$visibilty = "Public / Private"
#add more variables for Team creation if needed

$channels = @("Channel 1", "Channel 2");
$members = @("MeganB@proteamsdev.onmicrosoft.com");

#Create Team
Write-Host "Creating Team"
$team = New-Team -DisplayName $name -Description $description -MailNickName $mailNickName -Visibility $visibilty -AllowGiphy $true -GiphyContentRating "Moderate" -AllowStickersAndMemes $true -AllowCustomMemes $true -AllowGuestCreateUpdateChannels $true -AllowGuestDeleteChannels $true -AllowCreateUpdateChannels $true -AllowDeleteChannels $true -AllowAddRemoveApps $true -AllowCreateUpdateRemoveTabs $true -AllowCreateUpdateRemoveConnectors $true -AllowUserEditMessages $true -AllowUserDeleteMessages $true -AllowOwnerDeleteMessages $true -AllowTeamMentions $true -AllowChannelMentions $true


#Add channels
foreach ($element in $channels) {
    Write-Host "Adding channel " $element 
    New-TeamChannel -GroupId $team.GroupId -DisplayName $element
}

#add users
foreach ($element in $members) {
    Write-Host "Adding member " $element 
    Add-TeamUser -GroupId $team.GroupId -User $element
}