set shell := ["bash", "-cu"]

default:
	just --list

db-update:
	cd backend && echo 2 | pwsh ./ManageMigration.ps1

backend:
	dotnet run --project backend/XActBackend/XActBackend.csproj

frontend api_base_url="http://localhost:5200":
	cd xact_frontend && flutter run --dart-define=API_BASE_URL={{api_base_url}}

apk api_base_url="http://localhost:5200":
	cd xact_frontend && flutter build apk --dart-define=API_BASE_URL={{api_base_url}}
