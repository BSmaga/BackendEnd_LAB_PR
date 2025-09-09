# tests/smoke.ps1
$base = "http://localhost:5080"

Write-Host "1) Login admin..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Uri "$base/api/auth/login?email=admin@lib.pl&haslo=Pass!23" -Method Post
$token = $login.token
Write-Host "   OK, token: $($token.Substring(0,20))..." -ForegroundColor Green

Write-Host "2) GET /api/ksiazki" -ForegroundColor Cyan
$books = Invoke-RestMethod -Uri "$base/api/ksiazki?page=1&pageSize=5" `
  -Headers @{Authorization="Bearer $token"} -Method Get
$books | ConvertTo-Json -Depth 5

Write-Host "3) POST /api/ksiazki (Admin)" -ForegroundColor Cyan
$body = @{
  tytul="Test"
  autor="Autor"
  rok=2025
  isbn="TEST-ISBN"
  liczbaEgzemplarzy=1
} | ConvertTo-Json
$new = Invoke-RestMethod -Uri "$base/api/ksiazki" `
  -Headers @{Authorization="Bearer $token"; "Content-Type"="application/json"} `
  -Method Post -Body $body
$new | ConvertTo-Json

Write-Host "4) DELETE /api/ksiazki/$($new.id) (Admin)" -ForegroundColor Cyan
Invoke-RestMethod -Uri "$base/api/ksiazki/$($new.id)" `
  -Headers @{Authorization="Bearer $token"} -Method Delete
Write-Host "   Usunięto." -ForegroundColor Green
