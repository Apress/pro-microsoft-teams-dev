#Simple command
#New-Team -DisplayName "My New Team"

#Fully configured Team
New-Team -DisplayName "My Configured Team" -Description "This is a configured team" -MailNickName "configured" -Visibility "Public" -AllowGiphy $true -GiphyContentRating "Moderate" -AllowStickersAndMemes $true -AllowCustomMemes $true -AllowGuestCreateUpdateChannels $true -AllowGuestDeleteChannels $true -AllowCreateUpdateChannels $true -AllowDeleteChannels $true -AllowAddRemoveApps $true -AllowCreateUpdateRemoveTabs $true -AllowCreateUpdateRemoveConnectors $true -AllowUserEditMessages $true -AllowUserDeleteMessages $true -AllowOwnerDeleteMessages $true -AllowTeamMentions $true -AllowChannelMentions $true