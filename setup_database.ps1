# SchoolPC MySQL Database Setup
# Make sure XAMPP MySQL is running before executing this script

Write-Host "=== SchoolPC Database Setup ===" -ForegroundColor Cyan
Write-Host ""

# Configuration
$mysqlPath = "C:\xampp\mysql\bin\mysql.exe"
$host = "localhost"
$port = 3306
$user = "root"
$password = ""  # Default XAMPP has no root password

# Check if MySQL executable exists
if (-not (Test-Path $mysqlPath)) {
	Write-Host "ERROR: MySQL not found at $mysqlPath" -ForegroundColor Red
	Write-Host "Please update the path in this script or use phpMyAdmin instead." -ForegroundColor Yellow
	Write-Host ""
	Write-Host "Alternative: Open phpMyAdmin (http://localhost/phpmyadmin)" -ForegroundColor Yellow
	Write-Host "Then go to SQL tab and paste the contents of database_setup.sql" -ForegroundColor Yellow
	pause
	exit
}

# Check if database_setup.sql exists
if (-not (Test-Path "database_setup.sql")) {
	Write-Host "ERROR: database_setup.sql not found in current directory" -ForegroundColor Red
	pause
	exit
}

Write-Host "Found MySQL at: $mysqlPath" -ForegroundColor Green
Write-Host "Executing database_setup.sql..." -ForegroundColor Yellow
Write-Host ""

# Execute SQL script
try {
	& $mysqlPath -h $host -P $port -u $user -e "source database_setup.sql"

	Write-Host ""
	Write-Host "=== Database Setup Complete! ===" -ForegroundColor Green
	Write-Host ""
	Write-Host "Test Credentials:" -ForegroundColor Cyan
	Write-Host "  Username: jdoe       | Password: password123" -ForegroundColor White
	Write-Host "  Username: msmith     | Password: password123" -ForegroundColor White
	Write-Host "  Username: uic_student| Password: uic2024" -ForegroundColor White
	Write-Host ""
	Write-Host "Admin Credentials:" -ForegroundColor Cyan
	Write-Host "  Username: admin      | Password: admin123" -ForegroundColor White
	Write-Host ""
}
catch {
	Write-Host "ERROR: Failed to execute SQL script" -ForegroundColor Red
	Write-Host $_.Exception.Message -ForegroundColor Red
	Write-Host ""
	Write-Host "Manual Method:" -ForegroundColor Yellow
	Write-Host "1. Open phpMyAdmin: http://localhost/phpmyadmin" -ForegroundColor White
	Write-Host "2. Click 'SQL' tab" -ForegroundColor White
	Write-Host "3. Copy and paste the contents of database_setup.sql" -ForegroundColor White
	Write-Host "4. Click 'Go' button" -ForegroundColor White
}

Write-Host ""
pause
