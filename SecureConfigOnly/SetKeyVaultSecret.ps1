#  .\SetKeyVaultSecret.ps1 'D:\BGRS\Code\SecureConfigOnly\SecureConfigOnly\Secure-KV-PROD-SECRETS.json'  'Secure-KV-PROD' 'Secure-KV-PROD-SECRETS'

    Param
    (
         [Parameter(Mandatory=$true, Position=0)]
         [string] $FileName,
         [Parameter(Mandatory=$true, Position=1)]
         [string] $VaultName,
         [Parameter(Mandatory=$true, Position=2)]
         [string] $SecretName    
    )

    # Get the base64 encoding of the contents of the file
    $base64value = & .\CopySecretsToClipboard.ps1 $FileName $true

    # Before we can set the secret value we must convert it to a secure string
    $secretvalue = ConvertTo-SecureString $base64value -AsPlainText -Force

    # Set the secret value in the KeyVault
    Set-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName -SecretValue $secretvalue
