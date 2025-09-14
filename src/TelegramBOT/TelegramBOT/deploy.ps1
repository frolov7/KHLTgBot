# deploy.ps1

Write-Host "🛑 Stopping bot in PM2..."
pm2 stop telegram-bot

Start-Sleep -Seconds 2  # маленькая пауза, чтобы процесс освободил файлы

Write-Host "🧹 Cleaning old publish folder..."
if (Test-Path "./publish") {
    Remove-Item -Recurse -Force "./publish"
    Write-Host "✅ Old publish folder removed."
}

Write-Host "📂 Creating new publish folder..."
New-Item -ItemType Directory -Path "./publish" | Out-Null

Write-Host "🔨 Building project..."
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build success. Restarting bot in PM2..."
    pm2 restart telegram-bot
} else {
    Write-Host "❌ Build failed!"
    exit 1
}
